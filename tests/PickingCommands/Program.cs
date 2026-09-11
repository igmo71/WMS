using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Movements;
using Wms.Application.ShippingOrders;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

var database = "WmsPickingCommands_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
PickingCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), NullLogger<PickingCommandService>.Instance);
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "picking-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid(); var addContext = Context(); var deleteContext = Context();
    await Receipt("shipping-order.add-picking-movement", addContext, missing, $"{missing:N}|1|{missing:N}|1.25");
    await Receipt("shipping-order.delete-picking-movement", deleteContext, missing, $"{missing:N}|{missing:N}");
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Value(await commands.AddPickingMovementAsync(new(missing, 1, missing, 1.250m), addContext));
    Value(await commands.DeletePickingMovementAsync(new(missing, missing), deleteContext));
    Expect(await commands.AddPickingMovementAsync(new(missing, 1, missing, 2m), addContext), OperationErrorType.Conflict);
    Expect(await commands.DeletePickingMovementAsync(new(Guid.NewGuid(), missing), deleteContext), OperationErrorType.Conflict);
    Expect(await commands.AddPickingMovementAsync(new(missing, 1, missing, 1m), new(Guid.Empty, "picking-test")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: legacy add/delete receipts, invariant hashes, replay before lookup and input conflicts.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Picking" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var shipping = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "H", "Shipping", ZoneType.Shipping));
    var storage = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "S", "Storage", ZoneType.Storage));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    StorageLocation Location(Zone zone, int number) => Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, number, number.ToString(), details));
    var destination = Location(shipping, 1); var first = Location(storage, 1); var second = Location(storage, 2);
    setup.AddRange(warehouse, sku, shipping, storage, destination, first, second,
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, first.Id, sku.Id, 10m, DateTimeOffset.UtcNow)),
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, second.Id, sku.Id, 5m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    var id = await Order(); var add = new AddPickingMovementCommand(id, 1, first.Id, 4m); addContext = Context();
    var movement = Value(await commands.AddPickingMovementAsync(add, addContext));
    Check(Value(await commands.AddPickingMovementAsync(add, addContext)) == movement, "Add replay"); await Verify(id, 1, 4m);
    var update = new UpdatePickingMovementCommand(id, movement, second.Id, 3.125m); var updateContext = Context();
    Value(await commands.UpdatePickingMovementAsync(update, updateContext));
    Value(await commands.UpdatePickingMovementAsync(update with { Quantity = 3.1250m }, updateContext)); await Verify(id, 1, 3.125m);
    Expect(await commands.UpdatePickingMovementAsync(update with { Quantity = 4m }, updateContext), OperationErrorType.Conflict);
    var wrong = Context(); Expect(await commands.UpdatePickingMovementAsync(update with { OrderId = Guid.NewGuid() }, wrong), OperationErrorType.NotFound); await NoReceipt(wrong);
    var wrongDelete = Context(); Expect(await commands.DeletePickingMovementAsync(new(Guid.NewGuid(), movement), wrongDelete), OperationErrorType.NotFound); await NoReceipt(wrongDelete);
    deleteContext = Context(); Value(await commands.DeletePickingMovementAsync(new(id, movement), deleteContext)); Value(await commands.DeletePickingMovementAsync(new(id, movement), deleteContext));
    Value(await commands.AddPickingMovementAsync(add, addContext)); Value(await commands.UpdatePickingMovementAsync(update, updateContext)); await Verify(id, 0, 0m);
    foreach (var quantity in new[] { 0m, -1m, 0.0001m, 11m })
    {
        var rejected = Context(); Expect(await commands.AddPickingMovementAsync(new(id, 1, first.Id, quantity), rejected), OperationErrorType.Invalid); await NoReceipt(rejected);
    }
    var overstock = Context(); Expect(await commands.AddPickingMovementAsync(new(id, 1, second.Id, 6m), overstock), OperationErrorType.Invalid); await NoReceipt(overstock);
    var wrongZone = Context(); Expect(await commands.AddPickingMovementAsync(new(id, 1, destination.Id, 1m), wrongZone), OperationErrorType.Invalid); await NoReceipt(wrongZone);
    await using (var db = factory.CreateDbContext())
    {
        db.StorageLocationLocks.Add(Value(StorageLocationLock.CreateManual(first.Id, "Test lock", DateTimeOffset.UtcNow, "picking-test"))); await db.SaveChangesAsync();
    }
    var lockContext = Context(); Check(!(await commands.AddPickingMovementAsync(add, lockContext)).IsSuccess, "Locked source"); await NoReceipt(lockContext); await Verify(id, 0, 0m);
    await using (var db = factory.CreateDbContext()) { db.StorageLocationLocks.Remove(await db.StorageLocationLocks.SingleAsync()); await db.SaveChangesAsync(); }
    movement = Value(await commands.AddPickingMovementAsync(add, lockContext));
    Value(await commands.AddPickingMovementAsync(new(id, 1, second.Id, 5m), Context())); await Verify(id, 2, 9m);
    var allocated = Context(); Expect(await commands.AddPickingMovementAsync(new(id, 1, first.Id, 2m), allocated), OperationErrorType.Invalid); await NoReceipt(allocated);
    await using (var db = factory.CreateDbContext())
    {
        Check(await db.InventoryBalances.SumAsync(x => x.Quantity) == 15m && !await db.InventoryTurnovers.AnyAsync(), "Drafts do not post stock");
        var order = await db.ShippingOrders.Include(x => x.Items).SingleAsync(x => x.Id == id);
        var drafts = await db.InventoryMovements.Where(x => x.RecorderId == id).ToListAsync();
        Require(order.SetReadyForShipment(drafts, DateTimeOffset.UtcNow, "picking-test")); await db.SaveChangesAsync();
    }
    Value(await commands.AddPickingMovementAsync(add, lockContext));
    Expect(await commands.UpdatePickingMovementAsync(new(id, movement, first.Id, 1m), Context()), OperationErrorType.Invalid);
    Expect(await commands.DeletePickingMovementAsync(new(id, movement), Context()), OperationErrorType.Invalid);
    Console.WriteLine("PASS: split allocation/fact recalculation, replay after deletion/closure, invalid input/stock/lock rejection and retry; no posting.");

    id = await Order(); add = new(id, 1, first.Id, 4m); var shared = Context(); var concurrent = Race();
    var same = await Task.WhenAll(concurrent.AddPickingMovementAsync(add, shared), concurrent.AddPickingMovementAsync(add, shared));
    movement = Value(same[0]); Check(movement == Value(same[1]), "Concurrent add winning receipt"); await Verify(id, 1, 4m);
    concurrent = Race();
    var distinct = await Task.WhenAll(concurrent.AddPickingMovementAsync(new(id, 1, first.Id, 6m), Context()), concurrent.AddPickingMovementAsync(new(id, 1, first.Id, 6m), Context()));
    Check(distinct.Count(x => x.IsSuccess) == 1, "One allocation winner"); Expect(distinct.Single(x => !x.IsSuccess), OperationErrorType.Conflict); await Verify(id, 2, 10m);
    concurrent = Race();
    var edits = await Task.WhenAll(concurrent.UpdatePickingMovementAsync(new(id, movement, second.Id, 4m), Context()), concurrent.DeletePickingMovementAsync(new(id, movement), Context()));
    Check(edits.Count(x => x.IsSuccess) == 1, "One update/delete winner"); Expect(edits.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    await Verify(id, edits[0].IsSuccess ? 2 : 1, edits[0].IsSuccess ? 10m : 6m);
    // A duplicate deletion can replay even when SQL reports the disappeared row first.
    id = await Order(); movement = Value(await commands.AddPickingMovementAsync(new(id, 1, first.Id, 1m), Context()));
    shared = Context(); concurrent = Race();
    var deletes = await Task.WhenAll(concurrent.DeletePickingMovementAsync(new(id, movement), shared), concurrent.DeletePickingMovementAsync(new(id, movement), shared));
    Check(Value(deletes[0]) == Value(deletes[1]), "Concurrent delete winning receipt"); await Verify(id, 0, 0m);
    Console.WriteLine("PASS: concurrent duplicate add/delete, distinct allocation and update/delete conflicts, atomic facts.");

    PickingCommandService Race() => Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    async Task<Guid> Order()
    {
        var snapshot = new ShippingOrderImportSnapshot(Guid.NewGuid(), false, true, "Picking", DateTime.UtcNow,
            warehouse.Id, null, ShippingOrderStatus.Prepared, default, null, null, WarehouseOperation.CustomerShipment, Guid.NewGuid(), default,
            [new(1, sku.Id, 10m, 10m, ShippingOrderAction.PickUp)], [new(1, sku.Id, 10m, Guid.NewGuid(), "CustomerOrder")]);
        var order = Value(ShippingOrder.Create(snapshot, DateTimeOffset.UtcNow));
        Require(order.SetShippingLocation(destination.Id)); Require(order.SetReadyForPicking(DateTimeOffset.UtcNow, "picking-test"));
        await using var db = factory.CreateDbContext(); db.Add(order); await db.SaveChangesAsync(); return order.Id;
    }
    async Task Verify(Guid orderId, int count, decimal fact)
    {
        await using var db = factory.CreateDbContext();
        Check((await db.ShippingOrders.Include(x => x.Items).SingleAsync(x => x.Id == orderId)).Items.Single().FactQuantity == fact, "Fact matches drafts");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId) == count, "Draft count");
        Check(!await db.InventoryMovements.AnyAsync(x => x.RecorderId == orderId && x.PostedAtUtc != null), "No posted movements");
    }
    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
    async Task Receipt(string type, CommandContext c, Guid result, string input)
    {
        await using var db = factory.CreateDbContext(); db.CommandReceipts.Add(new CommandReceipt { UserId = c.UserId, RequestId = c.RequestId,
            CommandType = type, ResultResourceId = result, RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))), CompletedAtUtc = DateTimeOffset.UtcNow });
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
