using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Movements;
using Wms.Application.ReceivingOrders;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

var database = "WmsPutawayCommands_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
PutawayCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), new InventoryPostingService(NullLogger<InventoryPostingService>.Instance), NullLogger<PutawayCommandService>.Instance);
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "putaway-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid(); var contexts = Enumerable.Range(0, 4).Select(_ => Context()).ToArray();
    var n = missing.ToString("N");
    string[] types = ["start-putaway", "add-putaway-movement", "delete-putaway-movement", "complete-putaway"];
    string[] inputs = [n, $"{n}|1|{n}|1.25", $"{n}|{n}", n];
    for (int i = 0; i < contexts.Length; i++)
        setup.CommandReceipts.Add(new CommandReceipt { UserId = contexts[i].UserId, RequestId = contexts[i].RequestId,
            CommandType = "receiving-order." + types[i], RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputs[i]))),
            ResultResourceId = missing, CompletedAtUtc = DateTimeOffset.UtcNow });
    await setup.SaveChangesAsync();
    Value(await commands.StartAsync(missing, contexts[0]));
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Value(await commands.AddMovementAsync(new(missing, 1, missing, 1.250m), contexts[1]));
    Value(await commands.DeleteMovementAsync(new(missing, missing), contexts[2]));
    Value(await commands.CompleteAsync(missing, contexts[3]));
    Expect(await commands.AddMovementAsync(new(missing, 1, missing, 2m), contexts[1]), OperationErrorType.Conflict);
    Expect(await commands.DeleteMovementAsync(new(Guid.NewGuid(), missing), contexts[2]), OperationErrorType.Conflict);
    Expect(await commands.StartAsync(missing, new(Guid.Empty, "putaway-test")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: four legacy receipt types, invariant hashes, replay before lookup and changed-input conflicts.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Putaway" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var receiving = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "R", "Receiving", ZoneType.Receiving));
    var storage = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "S", "Storage", ZoneType.Storage));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    StorageLocation Location(Zone zone, int number) => Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, number, number.ToString(), details));
    var source = Location(receiving, 1); var first = Location(storage, 1); var second = Location(storage, 2);
    setup.AddRange(warehouse, sku, receiving, storage, source, first, second,
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, source.Id, sku.Id, 100m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    var id = await Order(); var startContext = Context();
    Value(await commands.StartAsync(id, startContext)); Value(await commands.StartAsync(id, startContext));
    var add = new AddPutawayMovementCommand(id, 1, first.Id, 4m); var addContext = Context();
    var movement = Value(await commands.AddMovementAsync(add, addContext));
    Check(Value(await commands.AddMovementAsync(add, addContext)) == movement, "Add replay");
    var update = new UpdatePutawayMovementCommand(id, movement, second.Id, 3m); var updateContext = Context();
    Value(await commands.UpdateMovementAsync(update, updateContext)); Value(await commands.UpdateMovementAsync(update, updateContext));
    Expect(await commands.UpdateMovementAsync(update with { Quantity = 4m }, updateContext), OperationErrorType.Conflict);
    var wrong = Context(); Expect(await commands.UpdateMovementAsync(update with { OrderId = Guid.NewGuid() }, wrong), OperationErrorType.NotFound); await NoReceipt(wrong);
    var wrongDelete = Context(); Expect(await commands.DeleteMovementAsync(new(Guid.NewGuid(), movement), wrongDelete), OperationErrorType.NotFound); await NoReceipt(wrongDelete);
    var deleteContext = Context(); Value(await commands.DeleteMovementAsync(new(id, movement), deleteContext)); Value(await commands.DeleteMovementAsync(new(id, movement), deleteContext));
    // Replay of earlier add/update remains successful even after resource deletion.
    Value(await commands.AddMovementAsync(add, addContext)); Value(await commands.UpdateMovementAsync(update, updateContext));
    await Verify(id, 0, 0, PutawayStatus.InProgress);
    var finishContext = Context(); Expect(await commands.CompleteAsync(id, finishContext), OperationErrorType.Invalid); await NoReceipt(finishContext);
    movement = Value(await commands.AddMovementAsync(new(id, 1, first.Id, 4m), Context()));
    Value(await commands.AddMovementAsync(new(id, 1, second.Id, 6m), Context()));
    var excess = Context(); Expect(await commands.AddMovementAsync(new(id, 1, first.Id, 1m), excess), OperationErrorType.Invalid); await NoReceipt(excess);
    await Verify(id, 2, 0, PutawayStatus.InProgress);
    await using (var db = factory.CreateDbContext())
    {
        Check((await db.InventoryBalances.SingleAsync()).Quantity == 100m && !await db.InventoryTurnovers.AnyAsync(), "Drafts never change stock");
        db.StorageLocationLocks.Add(Value(StorageLocationLock.CreateManual(second.Id, "Test lock", DateTimeOffset.UtcNow, "putaway-test")));
        await db.SaveChangesAsync();
    }
    Check(!(await commands.CompleteAsync(id, finishContext)).IsSuccess, "Revalidate destination lock"); await NoReceipt(finishContext);
    await Verify(id, 2, 0, PutawayStatus.InProgress);
    await using (var db = factory.CreateDbContext())
    {
        db.StorageLocationLocks.Remove(await db.StorageLocationLocks.SingleAsync());
        Value((await db.InventoryBalances.SingleAsync()).Adjust(-95m, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }
    Check(!(await commands.CompleteAsync(id, finishContext)).IsSuccess, "Revalidate source stock"); await NoReceipt(finishContext);
    await Verify(id, 2, 0, PutawayStatus.InProgress);
    await using (var db = factory.CreateDbContext())
    {
        Value((await db.InventoryBalances.SingleAsync()).Adjust(95m, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
    }
    Value(await commands.CompleteAsync(id, finishContext)); Value(await commands.CompleteAsync(id, finishContext));
    await Verify(id, 2, 2, PutawayStatus.Completed);
    await using (var db = factory.CreateDbContext())
    {
        Check(await db.InventoryTurnovers.CountAsync() == 4, "Two turnovers per posted movement");
        Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == source.Id)).Quantity == 90m, "Source deducted once");
        Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == first.Id)).Quantity == 4m, "First split");
        Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == second.Id)).Quantity == 6m, "Second split");
    }
    Expect(await commands.UpdateMovementAsync(new(id, movement, second.Id, 1m), Context()), OperationErrorType.Invalid);
    Expect(await commands.DeleteMovementAsync(new(id, movement), Context()), OperationErrorType.Invalid);
    Value(await commands.StartAsync(id, startContext));
    Console.WriteLine("PASS: draft add/update/delete replay, split allocations, lock/stock revalidation, atomic retry and once-only posting.");

    id = await Order(); var shared = Context(); var concurrent = Race();
    var starts = await Task.WhenAll(concurrent.StartAsync(id, shared), concurrent.StartAsync(id, shared));
    Check(Value(starts[0]) == Value(starts[1]), "Concurrent start winning receipt");
    shared = Context(); concurrent = Race(); add = new(id, 1, first.Id, 4m);
    var adds = await Task.WhenAll(concurrent.AddMovementAsync(add, shared), concurrent.AddMovementAsync(add, shared));
    movement = Value(adds[0]); Check(movement == Value(adds[1]), "Concurrent add winning receipt");
    concurrent = Race();
    var distinct = await Task.WhenAll(concurrent.AddMovementAsync(new(id, 1, first.Id, 6m), Context()), concurrent.AddMovementAsync(new(id, 1, second.Id, 6m), Context()));
    Check(distinct.Count(x => x.IsSuccess) == 1, "Concurrent distinct additions do not overallocate");
    Expect(distinct.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    await Verify(id, 2, 0, PutawayStatus.InProgress);
    shared = Context(); concurrent = Race();
    var completes = await Task.WhenAll(concurrent.CompleteAsync(id, shared), concurrent.CompleteAsync(id, shared));
    Check(Value(completes[0]) == Value(completes[1]), "Concurrent complete winning receipt"); await Verify(id, 2, 2, PutawayStatus.Completed);
    id = await Order(); Value(await commands.StartAsync(id, Context()));
    movement = Value(await commands.AddMovementAsync(new(id, 1, first.Id, 10m), Context()));
    concurrent = Race();
    var editRace = await Task.WhenAll(
        concurrent.UpdateMovementAsync(new(id, movement, second.Id, 10m), Context()),
        concurrent.DeleteMovementAsync(new(id, movement), Context()));
    Check(editRace.Count(x => x.IsSuccess) == 1, "Concurrent edit/delete has one winner");
    Expect(editRace.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    await using (var db = factory.CreateDbContext()) Check(await db.InventoryBalances.SumAsync(x => x.Quantity) == 100m, "Stock conserved");
    Console.WriteLine("PASS: simultaneous duplicate start/add/complete and distinct allocation conflicts; conserved stock.");

    PutawayCommandService Race() => Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    async Task<Guid> Order()
    {
        var snapshot = new ReceivingOrderImportSnapshot(Guid.NewGuid(), false, true, "Putaway", DateTime.UtcNow, warehouse.Id, null,
            ReceivingOrderStatus.ReadyForReceiving, default, WarehouseOperation.VendorReceipt, BusinessOperation.VendorPurchase,
            Guid.NewGuid(), default, Guid.NewGuid(), null, [new(1, sku.Id, 10m, 10m, null)]);
        var order = Value(ReceivingOrder.Create(snapshot, DateTimeOffset.UtcNow));
        Require(order.SetReceivingLocation(source.Id)); Require(order.SetInReceiving(DateTimeOffset.UtcNow, "putaway-test"));
        Require(order.UpdateItemFactQuantity(1, 10m)); Require(order.SetReceived(DateTimeOffset.UtcNow, "putaway-test"));
        await using var db = factory.CreateDbContext(); db.Add(order); await db.SaveChangesAsync(); return order.Id;
    }
    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
    async Task Verify(Guid orderId, int movementCount, int posted, PutawayStatus state)
    {
        await using var db = factory.CreateDbContext();
        Check((await db.ReceivingOrders.SingleAsync(x => x.Id == orderId)).PutawayStatus == state, "State");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId) == movementCount, "Movement count");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId && x.PostedAtUtc != null) == posted, "Posted count");
        Check(await db.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == orderId) == posted * 2, "Atomic turnover count");
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
