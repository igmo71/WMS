using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Movements;
using Wms.Application.ShippingOrders;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

// No application connection string: create/delete only this disposable LocalDB database.
var database = "WmsShippingCommands_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection()
    .Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
    .BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true")
    .Options;
var factory = new ContextFactory(options);
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var source = new Source();
    var sink = new Sink(source);
    var commandService = new ShippingOrderCommandService(factory, new CommandExecutor(factory),
        new InventoryPostingService(NullLogger<InventoryPostingService>.Instance),
        new ShippingOrderSynchronizationService(factory, source, NullLogger<ShippingOrderSynchronizationService>.Instance),
        sink, NullLogger<ShippingOrderCommandService>.Instance);
    var picking = new PickingCommandService(factory, NullLogger<PickingCommandService>.Instance);
    const string user = "shipping-test";

    // Seed old protocol strings/hashes without using the new command implementation.
    var missingOrder = Guid.NewGuid();
    var missingLocation = Guid.NewGuid();
    var oldStart = Context();
    var oldReady = Context();
    var oldShip = Context();
    await Receipt("shipping-order.start-picking", oldStart, $"{missingOrder:N}|{missingLocation:N}");
    await Receipt("shipping-order.complete-picking", oldReady, missingOrder.ToString("N"));
    await Receipt("shipping-order.ship", oldShip, missingOrder.ToString("N"));
    Require(await commandService.StartPickingAsync(new(missingOrder, missingLocation), oldStart));
    Require(await commandService.SetReadyForShipmentAsync(missingOrder, oldReady));
    Require(await commandService.SetShippedAsync(missingOrder, oldShip));
    Check(source.Calls == 0 && sink.Calls == 0, "Replay skips missing state and 1C");
    Expect(await commandService.StartPickingAsync(new(missingOrder, Guid.NewGuid()), oldStart), OperationErrorType.Conflict);
    Expect(await commandService.SetReadyForShipmentAsync(Guid.NewGuid(), oldReady), OperationErrorType.Conflict);
    Expect(await commandService.SetShippedAsync(Guid.NewGuid(), oldShip), OperationErrorType.Conflict);
    Expect(await commandService.StartPickingAsync(new(missingOrder, missingLocation), new(Guid.Empty, user)), OperationErrorType.Invalid);
    Expect(await commandService.SetShippedAsync(missingOrder, new(Guid.NewGuid(), "")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: all three legacy receipts replay without state/1C; semantic conflicts and caller validation.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Shipping test" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var shippingZone = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "SHIP", "Shipping", ZoneType.Shipping));
    var storageZone = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "STOCK", "Storage", ZoneType.Storage));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    var shippingLocation = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, shippingZone.Id, null, 1, "001", details));
    var storageLocation = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, storageZone.Id, null, 1, "001", details));
    setup.AddRange(warehouse, sku, shippingZone, storageZone, shippingLocation, storageLocation,
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, storageLocation.Id, sku.Id, 100m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();

    // Exercise normal execution and recovery after a 1C target has already applied.
    foreach (bool failTargets in new[] { false, true })
    {
        var snapshot = Snapshot();
        setup.Add(Value(ShippingOrder.Create(snapshot, DateTimeOffset.UtcNow)));
        await setup.SaveChangesAsync();
        var start = Context();
        Require(await commandService.StartPickingAsync(new(snapshot.Id, shippingLocation.Id), start));
        int calls = sink.Calls;
        Require(await commandService.StartPickingAsync(new(snapshot.Id, shippingLocation.Id), start));
        Check(sink.Calls == calls, "Start replay");
        Require(await picking.AddPickingMovementAsync(snapshot.Id, 1, storageLocation.Id, 5m));
        source.Snapshot = snapshot with { Status = ShippingOrderStatus.ReadyForPicking };
        var ready = Context();
        sink.FailReady = failTargets;
        if (failTargets)
        {
            Expect(await commandService.SetReadyForShipmentAsync(snapshot.Id, ready), OperationErrorType.Failure);
            await VerifyFailure(snapshot.Id, ready, ShippingOrderStatus.ReadyForPicking,
                ShippingOrderSynchronizationComparer.Compare(
                    await Load(snapshot.Id), snapshot with { Status = ShippingOrderStatus.ReadyForPicking }).Fingerprint,
                expectedPosted: 0);
            sink.FailReady = false;
            // The test sink already changed source to the exact ready target.
            Check(source.Snapshot!.Status == ShippingOrderStatus.ReadyForShipment, "Source ready target applied");
        }
        Require(await commandService.SetReadyForShipmentAsync(snapshot.Id, ready));
        calls = source.Calls + sink.Calls;
        var readyTarget = source.Snapshot!;
        source.Snapshot = null;
        Require(await commandService.SetReadyForShipmentAsync(snapshot.Id, ready));
        Check(source.Calls + sink.Calls == calls, "Ready replay skips checkpoint and target calls");
        source.Snapshot = readyTarget;
        await VerifyPosting(snapshot.Id, ShippingOrderStatus.ReadyForShipment, 1, 2);

        var ship = Context();
        sink.FailShip = failTargets;
        if (failTargets)
        {
            Expect(await commandService.SetShippedAsync(snapshot.Id, ship), OperationErrorType.Failure);
            await VerifyFailure(snapshot.Id, ship, ShippingOrderStatus.ReadyForShipment,
                ShippingOrderSynchronizationComparer.Compare(await Load(snapshot.Id), readyTarget).Fingerprint,
                expectedPosted: 1);
            sink.FailShip = false;
            Check(source.Snapshot!.Status == ShippingOrderStatus.Shipped, "Source shipped target applied");
        }
        Require(await commandService.SetShippedAsync(snapshot.Id, ship));
        calls = source.Calls + sink.Calls;
        source.Snapshot = null;
        Require(await commandService.SetShippedAsync(snapshot.Id, ship));
        Check(source.Calls + sink.Calls == calls, "Ship replay skips checkpoint and target calls");
        await VerifyPosting(snapshot.Id, ShippingOrderStatus.Shipped, 2, 3);
        await using var verify = factory.CreateDbContext();
        Check(await verify.CommandReceipts.CountAsync(x => x.ResultResourceId == snapshot.Id) == 3, "One receipt per transition");
        Check(await verify.InventoryBalances.Where(x => x.StorageLocationId == shippingLocation.Id).SumAsync(x => x.Quantity) == 0m,
            "Shipping location empty after dispatch");
    }
    await using (var verify = factory.CreateDbContext())
        Check(await verify.InventoryBalances.Where(x => x.StorageLocationId == storageLocation.Id).SumAsync(x => x.Quantity) == 90m,
            "Only two shipments consumed stock, despite retries");
    Console.WriteLine("PASS: picking/shipping posting, balances, turnover, independently saved checkpoints, exact 1C target retries.");

    // A blocking synchronization result persists without entering final effects.
    var blockedSnapshot = Snapshot();
    setup.Add(Value(ShippingOrder.Create(blockedSnapshot, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    Require(await commandService.StartPickingAsync(new(blockedSnapshot.Id, shippingLocation.Id), Context()));
    source.Snapshot = blockedSnapshot with { Status = ShippingOrderStatus.ReadyForPicking, DeletionMark = true };
    var blockedContext = Context();
    int beforeCalls = sink.Calls;
    Expect(await commandService.SetReadyForShipmentAsync(blockedSnapshot.Id, blockedContext), OperationErrorType.Conflict);
    Check(sink.Calls == beforeCalls, "Blocking checkpoint prevents external target calls");
    var blockedOrder = await Load(blockedSnapshot.Id);
    Check(blockedOrder.SynchronizationLevel == OrderSynchronizationLevel.Blocking, "Blocking assessment is durable");
    await using (var verify = factory.CreateDbContext())
        Check(!await verify.CommandReceipts.AnyAsync(x => x.RequestId == blockedContext.RequestId), "No receipt for blocked completion");

    foreach (bool sameRequest in new[] { true, false })
    {
        var snapshot = Snapshot();
        setup.Add(Value(ShippingOrder.Create(snapshot, DateTimeOffset.UtcNow)));
        await setup.SaveChangesAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int entered = 0;
        sink.StartGate = async () =>
        {
            if (Interlocked.Increment(ref entered) == 2) gate.SetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(20));
        };
        var first = Context();
        var second = sameRequest ? first : Context();
        var results = await Task.WhenAll(
            commandService.StartPickingAsync(new(snapshot.Id, shippingLocation.Id), first),
            commandService.StartPickingAsync(new(snapshot.Id, shippingLocation.Id), second));
        sink.StartGate = null;
        Check(results.Count(x => x.IsSuccess) == (sameRequest ? 2 : 1), "Concurrent start success count");
        if (!sameRequest) Expect(results.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
        await using var verify = factory.CreateDbContext();
        Check(await verify.CommandReceipts.CountAsync(x => x.ResultResourceId == snapshot.Id) == 1, "Only winning receipt persists");
    }
    Console.WriteLine("PASS: blocking checkpoint, identical concurrent start replay, distinct stale start conflict; no schema drift.");

    CommandContext Context() => new(Guid.NewGuid(), user);
    ShippingOrderImportSnapshot Snapshot() => new(Guid.NewGuid(), false, true, "Shipping", DateTime.UtcNow,
        warehouse.Id, null, ShippingOrderStatus.Prepared, default, null, null,
        WarehouseOperation.CustomerShipment, Guid.NewGuid(), default,
        [new(1, sku.Id, 5m, 5m, ShippingOrderAction.PickUp)],
        [new(1, sku.Id, 5m, Guid.NewGuid(), "CustomerOrder")]);
    async Task Receipt(string type, CommandContext context, string input)
    {
        setup.CommandReceipts.Add(new CommandReceipt
        {
            UserId = user, RequestId = context.RequestId, CommandType = type,
            RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))),
            ResultResourceId = missingOrder, CompletedAtUtc = DateTimeOffset.UtcNow
        });
        await setup.SaveChangesAsync();
    }
    async Task<ShippingOrder> Load(Guid orderId)
    {
        await using var db = factory.CreateDbContext();
        return await db.ShippingOrders.Include(x => x.Items).Include(x => x.BaseItems).SingleAsync(x => x.Id == orderId);
    }
    async Task VerifyFailure(Guid orderId, CommandContext context, ShippingOrderStatus status, string fingerprint, int expectedPosted)
    {
        await using var db = factory.CreateDbContext();
        var saved = await db.ShippingOrders.SingleAsync(x => x.Id == orderId);
        Check(saved.Status == status && saved.SynchronizationFingerprint == fingerprint, "Checkpoint saved, final state unchanged");
        Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == context.RequestId), "No success receipt on failure");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId && x.PostedAtUtc != null) == expectedPosted,
            "No final movements posted on failure");
    }
    async Task VerifyPosting(Guid orderId, ShippingOrderStatus status, int movements, int turnovers)
    {
        await using var db = factory.CreateDbContext();
        Check((await db.ShippingOrders.SingleAsync(x => x.Id == orderId)).Status == status, "Saved status");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == orderId && x.PostedAtUtc != null) == movements,
            "Posted movement count");
        Check(await db.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == orderId) == turnovers,
            "Immutable turnover count");
    }
}
finally
{
    await setup.Database.EnsureDeletedAsync();
}

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void Require(OperationResult result) => Check(result.IsSuccess, result.Error?.Message ?? "Expected success");
static T Value<T>(OperationResult<T> result) { Require(result); return result.Value!; }
static void Expect(OperationResult result, OperationErrorType type) => Check(result.Error?.Type == type, $"Expected {type}, got {result.Error}");
sealed class ContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => new(options);
}
sealed class Source : IShippingOrderSource
{
    public int Calls { get; private set; }
    public ShippingOrderImportSnapshot? Snapshot { get; set; }
    public Task<OperationResult<ShippingOrderImportSnapshot>> GetSnapshotAsync(Guid orderId, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult<OperationResult<ShippingOrderImportSnapshot>>(Snapshot ?? throw new Exception("Unexpected 1C access"));
    }
}
sealed class Sink(Source source) : IShippingOrderExecutionSink
{
    public int Calls { get; private set; }
    public bool FailReady { get; set; }
    public bool FailShip { get; set; }
    public Func<Task>? StartGate { get; set; }
    public async Task<OperationResult> SetReadyForPickingAsync(Guid orderId, CancellationToken ct)
    {
        Calls++;
        if (StartGate is { } gate) await gate();
        return OperationResult.Success();
    }
    public Task<OperationResult> UpdateItemsAsync(ShippingOrder order, CancellationToken ct)
    {
        Calls++;
        source.Snapshot = source.Snapshot! with
        {
            Items = order.Items.Select(x => new ShippingOrderItemImportSnapshot(x.LineNumber, x.StockKeepingUnitId,
                x.FactQuantity, x.FactQuantity, x.FactQuantity > 0 ? ShippingOrderAction.Ship : ShippingOrderAction.DoNotShip)).ToArray()
        };
        return Task.FromResult(OperationResult.Success());
    }
    public Task<OperationResult> SetReadyForShipmentAsync(Guid orderId, CancellationToken ct)
    {
        Calls++;
        source.Snapshot = source.Snapshot! with { Status = ShippingOrderStatus.ReadyForShipment, Posted = true };
        return Task.FromResult(FailReady ? OperationResult.Failure(OperationError.Failure("Ready response lost")) : OperationResult.Success());
    }
    public Task<OperationResult> SetShippedAsync(Guid orderId, CancellationToken ct)
    {
        Calls++;
        source.Snapshot = source.Snapshot! with { Status = ShippingOrderStatus.Shipped, Posted = true };
        return Task.FromResult(FailShip ? OperationResult.Failure(OperationError.Failure("Ship response lost")) : OperationResult.Success());
    }
}
