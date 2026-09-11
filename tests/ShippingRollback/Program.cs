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

var database = "WmsShippingRollback_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
ShippingOrderCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), new InventoryPostingService(NullLogger<InventoryPostingService>.Instance),
        new ShippingOrderSynchronizationService(f, new Source(), NullLogger<ShippingOrderSynchronizationService>.Instance),
        new Sink(), NullLogger<ShippingOrderCommandService>.Instance);
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "rollback-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid(); var legacy = Context();
    setup.CommandReceipts.Add(new CommandReceipt { UserId = legacy.UserId, RequestId = legacy.RequestId,
        CommandType = "shipping-order.rollback", RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{missing:N}|\"reason\""))),
        ResultResourceId = missing, CompletedAtUtc = DateTimeOffset.UtcNow });
    await setup.SaveChangesAsync();
    Value(await commands.RollbackAsync(new(missing, "reason"), legacy));
    Expect(await commands.RollbackAsync(new(missing, "other"), legacy), OperationErrorType.Conflict);
    Expect(await commands.RollbackAsync(new(missing, "reason"), new(Guid.Empty, "rollback-test")), OperationErrorType.Invalid);

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Rollback" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var shipping = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "H", "Shipping", ZoneType.Shipping));
    var storage = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "S", "Storage", ZoneType.Storage));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    var source = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, storage.Id, null, 1, "1", details));
    var destination = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, shipping.Id, null, 1, "1", details));
    setup.AddRange(warehouse, sku, shipping, storage, source, destination,
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, source.Id, sku.Id, 100m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    var picking = new PickingCommandService(new CommandExecutor(factory), NullLogger<PickingCommandService>.Instance);
    var snapshots = new Dictionary<Guid, ShippingOrderImportSnapshot>();
    var id = await Order(); await Pick(id, 4m);
    var invalid = Context(); Expect(await commands.RollbackAsync(new(id, "  "), invalid), OperationErrorType.Invalid); await NoReceipt(invalid);
    var context = Context(); var command = new RollbackShippingOrderCommand(id, " reason | original ");
    Value(await commands.RollbackAsync(command, context)); await RolledBack(id, 0);
    Value(await commands.RollbackAsync(command, context));
    Expect(await commands.RollbackAsync(command with { Reason = command.Reason.Trim() }, context), OperationErrorType.Conflict);
    Expect(await commands.RollbackAsync(command, Context()), OperationErrorType.Invalid);
    // An old successful request must never undo a later picking cycle.
    await Restart(id); await Pick(id, 2m); Value(await commands.RollbackAsync(command, context));
    await using (var db = factory.CreateDbContext())
    {
        var order = await db.ShippingOrders.Include(x => x.Items).SingleAsync(x => x.Id == id);
        Check(order.Status == ShippingOrderStatus.ReadyForPicking && order.Items.Single().FactQuantity == 2m, "Old receipt preserves new cycle");
    }
    Value(await commands.RollbackAsync(new(id, "second cycle"), Context())); await RolledBack(id, 0);
    Console.WriteLine("PASS: early receipt lookup, exact reason hash, draft rollback/audit, invalid reason/state and replay after restart.");

    id = await Order(); await Pick(id, 5m); await Post(id);
    context = Context(); command = new(id, "posted rollback");
    await using (var db = factory.CreateDbContext())
    {
        db.StorageLocationLocks.Add(Value(StorageLocationLock.CreateManual(source.Id, "Test lock", DateTimeOffset.UtcNow, "rollback-test"))); await db.SaveChangesAsync();
    }
    Check(!(await commands.RollbackAsync(command, context)).IsSuccess, "Locked compensation destination"); await NoReceipt(context); await StillPosted(id);
    await using (var db = factory.CreateDbContext()) { db.StorageLocationLocks.Remove(await db.StorageLocationLocks.SingleAsync()); await db.SaveChangesAsync(); }
    await AdjustShipping(-5m);
    Check(!(await commands.RollbackAsync(command, context)).IsSuccess, "Missing shipping stock"); await NoReceipt(context); await StillPosted(id);
    await AdjustShipping(5m);
    Value(await commands.RollbackAsync(command, context)); Value(await commands.RollbackAsync(command, context)); await RolledBack(id, 2);
    await Restart(id); await Pick(id, 3m); await Post(id);
    Value(await commands.RollbackAsync(command, context)); // Must not compensate this new posted cycle.
    await using (var db = factory.CreateDbContext()) Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == destination.Id)).Quantity == 3m, "Old replay preserves later posted stock");
    Value(await commands.RollbackAsync(new(id, "next posted cycle"), Context())); await RolledBack(id, 4);
    await using (var db = factory.CreateDbContext()) Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == source.Id)).Quantity == 100m, "Both cycles compensated exactly once");
    Console.WriteLine("PASS: posted compensation/history, lock/stock atomic failure and same-request recovery, repeated cycle isolation.");

    id = await Order(); await Pick(id, 2m); await Post(id); context = Context(); command = new(id, "same race");
    var concurrent = Race();
    var same = await Task.WhenAll(concurrent.RollbackAsync(command, context), concurrent.RollbackAsync(command, context));
    Check(Value(same[0]) == Value(same[1]), "Concurrent duplicate returns winner"); await RolledBack(id, 2);
    id = await Order(); await Pick(id, 2m); await Post(id); concurrent = Race();
    var distinct = await Task.WhenAll(concurrent.RollbackAsync(new(id, "first"), Context()), concurrent.RollbackAsync(new(id, "second"), Context()));
    Check(distinct.Count(x => x.IsSuccess) == 1, "One distinct rollback winner"); Expect(distinct.Single(x => !x.IsSuccess), OperationErrorType.Conflict); await RolledBack(id, 2);
    id = await Order(); var movementId = await Pick(id, 2m);
    var raceFactory = new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options);
    var rollbackRace = Service(raceFactory); var pickingRace = new PickingCommandService(new CommandExecutor(raceFactory), NullLogger<PickingCommandService>.Instance);
    var edit = await Task.WhenAll(rollbackRace.RollbackAsync(new(id, "edit race"), Context()), pickingRace.UpdatePickingMovementAsync(new(id, movementId, source.Id, 3m), Context()));
    Check(edit.Count(x => x.IsSuccess) == 1, "One rollback/edit winner"); Expect(edit.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    if (edit[0].IsSuccess) await RolledBack(id, 0);
    else
    {
        await using var db = factory.CreateDbContext();
        Check((await db.ShippingOrders.Include(x => x.Items).SingleAsync(x => x.Id == id)).Items.Single().FactQuantity == 3m, "Edit winner remains intact");
    }
    var shippedId = await Order(); await Pick(shippedId, 1m); await Post(shippedId);
    await using (var db = factory.CreateDbContext())
    {
        var order = await db.ShippingOrders.SingleAsync(x => x.Id == shippedId); Require(order.SetShipped(DateTimeOffset.UtcNow, "rollback-test")); await db.SaveChangesAsync();
    }
    var shippedContext = Context(); Expect(await commands.RollbackAsync(new(shippedId, "forbidden"), shippedContext), OperationErrorType.Invalid); await NoReceipt(shippedContext);
    Console.WriteLine("PASS: concurrent duplicate/distinct rollback, rollback/edit conflict and shipped prohibition; no 1C access.");

    ShippingOrderCommandService Race() => Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    async Task<Guid> Order()
    {
        var snapshot = new ShippingOrderImportSnapshot(Guid.NewGuid(), false, true, "Rollback", DateTime.UtcNow,
            warehouse.Id, null, ShippingOrderStatus.Prepared, default, null, null, WarehouseOperation.CustomerShipment, Guid.NewGuid(), default,
            [new(1, sku.Id, 10m, 10m, ShippingOrderAction.PickUp)], [new(1, sku.Id, 10m, Guid.NewGuid(), "CustomerOrder")]);
        var order = Value(ShippingOrder.Create(snapshot, DateTimeOffset.UtcNow));
        Require(order.SetShippingLocation(destination.Id)); Require(order.SetReadyForPicking(DateTimeOffset.UtcNow, "rollback-test"));
        snapshots[order.Id] = snapshot;
        await using var db = factory.CreateDbContext(); db.Add(order); await db.SaveChangesAsync(); return order.Id;
    }
    async Task Restart(Guid orderId)
    {
        await using var db = factory.CreateDbContext(); var order = await db.ShippingOrders.SingleAsync(x => x.Id == orderId);
        Require(order.SetShippingLocation(destination.Id)); Require(order.SetReadyForPicking(DateTimeOffset.UtcNow, "rollback-test")); await db.SaveChangesAsync();
    }
    async Task<Guid> Pick(Guid orderId, decimal quantity) => Value(await picking.AddPickingMovementAsync(new(orderId, 1, source.Id, quantity), Context()));
    async Task Post(Guid orderId)
    {
        // Prepare real posted stock through the public completion command, using
        // separate permissive 1C doubles. Rollback itself keeps throwing doubles.
        var preparation = new ShippingOrderCommandService(new CommandExecutor(factory),
            new InventoryPostingService(NullLogger<InventoryPostingService>.Instance),
            new ShippingOrderSynchronizationService(factory,
                new PreparationSource(snapshots[orderId] with { Status = ShippingOrderStatus.ReadyForPicking }),
                NullLogger<ShippingOrderSynchronizationService>.Instance),
            new PreparationSink(), NullLogger<ShippingOrderCommandService>.Instance);
        Value(await preparation.SetReadyForShipmentAsync(orderId, Context()));
    }
    async Task AdjustShipping(decimal delta)
    {
        await using var db = factory.CreateDbContext(); Value((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == destination.Id)).Adjust(delta, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
    }
    async Task RolledBack(Guid orderId, int history)
    {
        await using var db = factory.CreateDbContext(); var order = await db.ShippingOrders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        Check(order.Status == ShippingOrderStatus.Prepared && order.ShippingLocationId is null && order.Items.All(x => x.FactQuantity == 0), "Reset state/facts/location");
        Check(order.PickingStartedAtUtc is null && order.ReadyForShipmentAtUtc is null && order.PickingStartedBy is null && order.ReadyForShipmentBy is null && order.RolledBackBy == "rollback-test" && !string.IsNullOrWhiteSpace(order.RollbackReason), "Reset cycle audit; retain rollback audit");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId) == history && !await db.InventoryMovements.AnyAsync(x => x.RecorderId == orderId && x.PostedAtUtc == null), "Drafts removed, history retained");
        Check(await db.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == orderId) == history * 2, "Immutable turnover history");
    }
    async Task StillPosted(Guid orderId)
    {
        await using var db = factory.CreateDbContext(); Check((await db.ShippingOrders.SingleAsync(x => x.Id == orderId)).Status == ShippingOrderStatus.ReadyForShipment, "Failed rollback leaves state");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId) == 1 && await db.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == orderId) == 2, "No partial compensation");
    }
    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
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

sealed class Source : IShippingOrderSource
{
    public Task<OperationResult<ShippingOrderImportSnapshot>> GetSnapshotAsync(Guid orderId, CancellationToken ct = default) => throw new Exception("Rollback must not call 1C");
}
sealed class Sink : IShippingOrderExecutionSink
{
    public Task<OperationResult> SetReadyForPickingAsync(Guid orderId, CancellationToken ct) => throw new Exception("Rollback must not call 1C");
    public Task<OperationResult> UpdateItemsAsync(ShippingOrder order, CancellationToken ct) => throw new Exception("Rollback must not call 1C");
    public Task<OperationResult> SetReadyForShipmentAsync(Guid orderId, CancellationToken ct) => throw new Exception("Rollback must not call 1C");
    public Task<OperationResult> SetShippedAsync(Guid orderId, CancellationToken ct) => throw new Exception("Rollback must not call 1C");
}

sealed class PreparationSource(ShippingOrderImportSnapshot snapshot) : IShippingOrderSource
{
    public Task<OperationResult<ShippingOrderImportSnapshot>> GetSnapshotAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult<OperationResult<ShippingOrderImportSnapshot>>(snapshot);
}
sealed class PreparationSink : IShippingOrderExecutionSink
{
    public Task<OperationResult> SetReadyForPickingAsync(Guid orderId, CancellationToken ct) => Task.FromResult(OperationResult.Success());
    public Task<OperationResult> UpdateItemsAsync(ShippingOrder order, CancellationToken ct) => Task.FromResult(OperationResult.Success());
    public Task<OperationResult> SetReadyForShipmentAsync(Guid orderId, CancellationToken ct) => Task.FromResult(OperationResult.Success());
    public Task<OperationResult> SetShippedAsync(Guid orderId, CancellationToken ct) => Task.FromResult(OperationResult.Success());
}
