using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.ReceivingOrders;
using Wms.Application.Inventory.Movements;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

// No application connection string: only this uniquely named disposable database.
var database = "WmsReceivingFacts_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
ReceivingOrderCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), new InventoryPostingService(NullLogger<InventoryPostingService>.Instance),
        new ReceivingOrderSynchronizationService(f, new Source(), NullLogger<ReceivingOrderSynchronizationService>.Instance),
        new Sink(), NullLogger<ReceivingOrderCommandService>.Instance);
ReceivingOrderCommandService RacingService() => Service(new ContextFactory(
    new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "facts-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid(); var incrementContext = Context(); var setContext = Context();
    await Receipt("receiving-order.increment-fact", incrementContext, missing, $"{missing:N}|1");
    await Receipt("receiving-order.set-fact", setContext, missing, $"{missing:N}|1|1.25");
    Check(Value(await commands.IncrementItemFactAsync(new(missing, 1), incrementContext)) == missing, "Legacy increment replay");
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Value(await commands.SetItemFactQuantityAsync(new(missing, 1, 1.250m), setContext));
    Expect(await commands.IncrementItemFactAsync(new(missing, 2), incrementContext), OperationErrorType.Conflict);
    Expect(await commands.SetItemFactQuantityAsync(new(missing, 1, 2m), setContext), OperationErrorType.Conflict);
    Expect(await commands.IncrementItemFactAsync(new(missing, 1), new(Guid.Empty, "facts-test")), OperationErrorType.Invalid);
    Expect(await commands.IncrementItemFactAsync(new(missing, 1), new(Guid.NewGuid(), "")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: legacy increment/set hashes, replay before mutable lookup, changed input and caller validation.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Facts test" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var zone = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "R", "Receiving", ZoneType.Receiving));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    var location = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, 1, "1", details));
    setup.AddRange(warehouse, sku, zone, location); await setup.SaveChangesAsync();
    var orderId = await CreateOrder();
    var commentContext = Context();
    Value(await commands.SetItemCommentAsync(new(orderId, 1, "fresh | comment\ntext"), commentContext));
    Check((await Item()).FactQuantity is null, "Comment preserves unconfirmed fact");
    var scan = new IncrementReceivingFactCommand(orderId, 1); var scanContext = Context();
    Value(await commands.IncrementItemFactAsync(scan, scanContext)); Value(await commands.IncrementItemFactAsync(scan, scanContext));
    Check((await Item()).FactQuantity == 1m && (await Item()).Comment == "fresh | comment\ntext", "Increment once preserves comment");
    var absolute = new SetReceivingFactCommand(orderId, 1, 3.125m); var absoluteContext = Context();
    Value(await commands.SetItemFactQuantityAsync(absolute, absoluteContext));
    Value(await commands.SetItemFactQuantityAsync(absolute with { FactQuantity = 3.1250m }, absoluteContext));
    Check((await Item()).FactQuantity == 3.125m && (await Item()).Comment == "fresh | comment\ntext", "Absolute quantity preserves current comment");
    var clearComment = Context(); Value(await commands.SetItemCommentAsync(new(orderId, 1, null), clearComment));
    Expect(await commands.SetItemCommentAsync(new(orderId, 1, ""), clearComment), OperationErrorType.Conflict);
    Check((await Item()).FactQuantity == 3.125m && (await Item()).Comment is null, "Clear comment preserves fact");
    Value(await commands.SetItemCommentAsync(new(orderId, 1, "later"), Context()));
    Value(await commands.SetItemCommentAsync(new(orderId, 1, null), clearComment));
    Check((await Item()).Comment == "later", "Replay does not overwrite later comment");
    Value(await commands.SetItemFactQuantityAsync(new(orderId, 1, 0m), Context()));
    Value(await commands.SetItemFactQuantityAsync(absolute, absoluteContext));
    Check((await Item()).FactQuantity == 0m, "Explicit zero and replay preserves later fact");
    foreach (var quantity in new[] { -1m, 0.0001m, 1000000000000m })
    {
        var rejected = Context();
        Expect(await commands.SetItemFactQuantityAsync(new(orderId, 1, quantity), rejected), OperationErrorType.Invalid);
        await NoReceipt(rejected);
    }
    var absentLine = Context(); Expect(await commands.IncrementItemFactAsync(new(orderId, 999), absentLine), OperationErrorType.NotFound); await NoReceipt(absentLine);
    Check((await Item()).FactQuantity == 0m && (await Item()).Comment == "later", "Rejected edits have no effects");
    var readyId = await CreateOrder(false); var readyContext = Context();
    Expect(await commands.SetItemFactQuantityAsync(new(readyId, 1, 2m), readyContext), OperationErrorType.Invalid); await NoReceipt(readyContext);
    await using (var db = factory.CreateDbContext())
    {
        var order = await db.ReceivingOrders.Include(x => x.Items).SingleAsync(x => x.Id == readyId);
        Require(order.SetInReceiving(DateTimeOffset.UtcNow, "facts-test")); await db.SaveChangesAsync();
    }
    Value(await commands.SetItemFactQuantityAsync(new(readyId, 1, 2m), readyContext));
    Console.WriteLine("PASS: fact/comment isolation, null/zero, exact comment hash, invalid input/state without receipts, same-request retry.");

    var concurrent = RacingService(); var shared = Context();
    var same = await Task.WhenAll(concurrent.IncrementItemFactAsync(scan, shared), concurrent.IncrementItemFactAsync(scan, shared));
    Check(Value(same[0]) == Value(same[1]) && (await Item()).FactQuantity == 1m, "Concurrent duplicate scan increments once");
    concurrent = RacingService();
    var distinct = await Task.WhenAll(concurrent.IncrementItemFactAsync(scan, Context()), concurrent.IncrementItemFactAsync(scan, Context()));
    Check(distinct.Count(x => x.IsSuccess) == 1, "One concurrent distinct scan succeeds");
    Expect(distinct.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    Check((await Item()).FactQuantity == 2m, "No lost or duplicated increment");
    concurrent = RacingService(); var editContext = Context(); var noteContext = Context();
    var quantityEdit = new SetReceivingFactCommand(orderId, 1, 7m);
    var noteEdit = new SetReceivingItemCommentCommand(orderId, 1, "concurrent note");
    var edits = await Task.WhenAll(concurrent.SetItemFactQuantityAsync(quantityEdit, editContext), concurrent.SetItemCommentAsync(noteEdit, noteContext));
    Check(edits.Count(x => x.IsSuccess) == 1, "Concurrent fact/comment conflict uses order revision");
    Expect(edits.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    if (!edits[0].IsSuccess) Value(await commands.SetItemFactQuantityAsync(quantityEdit, editContext));
    if (!edits[1].IsSuccess) Value(await commands.SetItemCommentAsync(noteEdit, noteContext));
    Check((await Item()).FactQuantity == 7m && (await Item()).Comment == "concurrent note", "Retry combines fact and comment without loss");
    await using (var db = factory.CreateDbContext())
    {
        var order = await db.ReceivingOrders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        Require(order.SetReceived(DateTimeOffset.UtcNow, "facts-test")); await db.SaveChangesAsync();
    }
    Value(await commands.IncrementItemFactAsync(scan, shared));
    Value(await commands.SetItemFactQuantityAsync(quantityEdit, editContext));
    Value(await commands.SetItemCommentAsync(noteEdit, noteContext));
    Expect(await commands.IncrementItemFactAsync(scan, Context()), OperationErrorType.Invalid);
    Expect(await commands.SetItemCommentAsync(new(orderId, 1, "closed"), Context()), OperationErrorType.Invalid);
    await using (var db = factory.CreateDbContext())
    {
        Check(!await db.InventoryMovements.AnyAsync() && !await db.InventoryTurnovers.AnyAsync() && !await db.InventoryBalances.AnyAsync(), "Fact entry never posts inventory");
    }
    Console.WriteLine("PASS: concurrent duplicates/distinct edits, recovery, replay after closure; no inventory or 1C effects.");

    async Task<Guid> CreateOrder(bool inReceiving = true)
    {
        var snapshot = new ReceivingOrderImportSnapshot(Guid.NewGuid(), false, true, "Facts", DateTime.UtcNow, warehouse.Id, null,
            ReceivingOrderStatus.ReadyForReceiving, default, WarehouseOperation.VendorReceipt,
            BusinessOperation.VendorPurchase, Guid.NewGuid(), default, Guid.NewGuid(), null,
            [new(1, sku.Id, 5m, 5m, null)]);
        var order = Value(ReceivingOrder.Create(snapshot, DateTimeOffset.UtcNow));
        Require(order.SetReceivingLocation(location.Id));
        if (inReceiving) Require(order.SetInReceiving(DateTimeOffset.UtcNow, "facts-test"));
        await using var db = factory.CreateDbContext(); db.Add(order); await db.SaveChangesAsync(); return order.Id;
    }
    async Task<ReceivingOrderItem> Item()
    {
        await using var db = factory.CreateDbContext();
        return (await db.ReceivingOrders.Include(x => x.Items).SingleAsync(x => x.Id == orderId)).Items.Single();
    }
    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
    async Task Receipt(string type, CommandContext c, Guid result, string input)
    {
        await using var db = factory.CreateDbContext();
        db.CommandReceipts.Add(new CommandReceipt { UserId = c.UserId, RequestId = c.RequestId, CommandType = type,
            RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))), ResultResourceId = result, CompletedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }
}
finally { await setup.Database.EnsureDeletedAsync(); }

static void Require(OperationResult result) => Check(result.IsSuccess, result.Error?.Message ?? "Expected success");
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static T Value<T>(OperationResult<T> result) { Check(result.IsSuccess, result.Error?.Message ?? "Expected success"); return result.Value!; }
static void Expect(OperationResult result, OperationErrorType type) => Check(result.Error?.Type == type, $"Expected {type}, got {result.Error}");
sealed class ContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
{ public ApplicationDbContext CreateDbContext() => new(options); }
sealed class SaveBarrier : SaveChangesInterceptor
{
    private int arrivals;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<CommandReceipt>().Any(x => x.State == EntityState.Added))
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
        return result;
    }
}

sealed class Source : IReceivingOrderSource
{
    public Task<OperationResult<ReceivingOrderImportSnapshot>> GetSnapshotAsync(Guid orderId, CancellationToken ct = default) => throw new Exception("Fact command accessed 1C source");
}
sealed class Sink : IReceivingOrderExecutionSink
{
    public Task<OperationResult> SetInReceivingAsync(Guid orderId, CancellationToken ct) => throw new Exception("Fact command accessed 1C sink");
    public Task<OperationResult> UpdateItemsAsync(Guid orderId, IReadOnlyCollection<ReceivingOrderItem> items, CancellationToken ct) => throw new Exception("Fact command accessed 1C sink");
    public Task<OperationResult> SetReceivedAsync(Guid orderId, CancellationToken ct) => throw new Exception("Fact command accessed 1C sink");
}
