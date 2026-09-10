using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Movements;
using Wms.Application.ReceivingOrders;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

// Windows LocalDB only. Creates and deletes its own uniquely named disposable DB.
var database = "WmsReceivingPilot_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection()
    .Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
    .BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true")
    .Options;
var factory = new ContextFactory(options);
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No relational migration drift");
    await setup.GetService<IMigrator>().MigrateAsync("20260904151251_CreateInitialWmsSchema");
    var source = new Source();
    var sink = new Sink();
    var executor = new CommandExecutor(factory);
    var service = new ReceivingOrderCommandService(factory, executor,
        new InventoryPostingService(NullLogger<InventoryPostingService>.Instance),
        new ReceivingOrderSynchronizationService(factory, source, NullLogger<ReceivingOrderSynchronizationService>.Instance),
        sink, NullLogger<ReceivingOrderCommandService>.Instance);
    const string user = "pilot-user";

    // Seed the old SQL protocol directly, without the new hash implementation.
    var absentOrder = Guid.NewGuid();
    var absentLocation = Guid.NewGuid();
    var oldStart = new CommandContext(Guid.NewGuid(), user);
    var oldComplete = new CommandContext(Guid.NewGuid(), user);
    await InsertLegacyReceipt("receiving-order.start-receiving", oldStart, absentOrder,
        LegacyHash($"{absentOrder:N}|{absentLocation:N}"));
    await InsertLegacyReceipt("receiving-order.complete-receiving", oldComplete, absentOrder,
        LegacyHash(absentOrder.ToString("N")));
    await setup.Database.MigrateAsync();
    Console.WriteLine("PASS: schema migration preserves existing Mobile receipts; no model drift.");
    Require(await service.StartReceivingAsync(new(absentOrder, absentLocation), oldStart));
    Require(await service.CompleteReceivingAsync(new(absentOrder, null), oldComplete));
    Check(source.Calls == 0 && sink.Calls == 0, "Legacy replay bypasses missing mutable state and 1C");
    ExpectError(await service.StartReceivingAsync(new(absentOrder, Guid.NewGuid()), oldStart), OperationErrorType.Conflict);
    ExpectError(await service.CompleteReceivingAsync(new(absentOrder, absentLocation), oldComplete), OperationErrorType.Conflict);
    ExpectError(await service.StartReceivingAsync(new(absentOrder, absentLocation), new(Guid.Empty, user)), OperationErrorType.Invalid);
    ExpectError(await service.StartReceivingAsync(new(absentOrder, absentLocation), new(Guid.NewGuid(), "")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: old start/completion receipts, changed input, request/user validation, replay without 1C.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Pilot" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "Pilot SKU" };
    var zone = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "R", "Receiving", ZoneType.Receiving));
    var details = Value(StorageLocationDetails.Create("Receiving", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    var location = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, 1, "001", details));
    var otherLocation = Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, 2, "002", details));
    setup.AddRange(warehouse, sku, zone, location, otherLocation);
    await setup.SaveChangesAsync();

    // Both completion input shapes execute the same use case and post once.
    foreach (bool explicitLocation in new[] { false, true })
    {
        var snapshot = Snapshot();
        setup.ReceivingOrders.Add(Value(ReceivingOrder.Create(snapshot, DateTimeOffset.UtcNow)));
        await setup.SaveChangesAsync();
        var start = new CommandContext(Guid.NewGuid(), user);
        Require(await service.StartReceivingAsync(new(snapshot.Id, location.Id), start));
        int calls = sink.Calls;
        Require(await service.StartReceivingAsync(new(snapshot.Id, location.Id), start));
        Check(sink.Calls == calls, "Start replay has no external call");
        await using (var db = factory.CreateDbContext())
        {
            var order = await db.ReceivingOrders.Include(x => x.Items).SingleAsync(x => x.Id == snapshot.Id);
            Require(order.UpdateItemFactQuantity(1, 5m));
            await db.SaveChangesAsync();
        }
        source.Snapshot = snapshot with { Status = ReceivingOrderStatus.InReceiving };
        var completion = new CompleteReceivingCommand(snapshot.Id, explicitLocation ? otherLocation.Id : null);
        var context = new CommandContext(Guid.NewGuid(), user);
        Require(await service.CompleteReceivingAsync(completion, context));
        calls = source.Calls + sink.Calls;
        source.Snapshot = null;
        Require(await service.CompleteReceivingAsync(completion, context));
        Check(source.Calls + sink.Calls == calls, "Completion replay skips checkpoint and sink");
        await using var verify = factory.CreateDbContext();
        var saved = await verify.ReceivingOrders.SingleAsync(x => x.Id == snapshot.Id);
        Check(saved.Status == ReceivingOrderStatus.Received, "Final order state saved");
        Check(saved.ReceivingLocationId == (explicitLocation ? otherLocation.Id : location.Id), "Location semantics preserved");
        Check(await verify.InventoryMovements.CountAsync(x => x.RecorderId == snapshot.Id) == 1, "Exactly one movement");
        Check(await verify.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == snapshot.Id) == 1, "Exactly one turnover");
        Check(await verify.CommandReceipts.AnyAsync(x => x.RequestId == context.RequestId), "Completion receipt saved");
    }
    Console.WriteLine("PASS: both completion inputs, real posting/turnover, start and completion replay.");

    // Exercise the actual receiving aggregate's optimistic concurrency token.
    foreach (bool sameRequest in new[] { true, false })
    {
        var snapshot = Snapshot();
        setup.Add(Value(ReceivingOrder.Create(snapshot, DateTimeOffset.UtcNow)));
        await setup.SaveChangesAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int entered = 0;
        sink.StartGate = async () =>
        {
            if (Interlocked.Increment(ref entered) == 2) gate.SetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(20));
        };
        var first = new CommandContext(Guid.NewGuid(), user);
        var second = sameRequest ? first : new CommandContext(Guid.NewGuid(), user);
        var results = await Task.WhenAll(
            service.StartReceivingAsync(new(snapshot.Id, location.Id), first),
            service.StartReceivingAsync(new(snapshot.Id, location.Id), second));
        sink.StartGate = null;
        Check(results.Count(x => x.IsSuccess) == (sameRequest ? 2 : 1), "Receiving race success count");
        if (!sameRequest)
            ExpectError(results.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
        await using var verify = factory.CreateDbContext();
        Check(await verify.CommandReceipts.CountAsync(x => x.ResultResourceId == snapshot.Id) == 1,
            "Only the winning receiving attempt is recorded");
    }
    Console.WriteLine("PASS: identical receiving requests replay the winner; distinct attempts classify stale saves as conflicts.");

    // A successful checkpoint persists even when the subsequent 1C target fails.
    var failedSnapshot = Snapshot();
    var failedOrder = Value(ReceivingOrder.Create(failedSnapshot, DateTimeOffset.UtcNow));
    Require(failedOrder.SetReceivingLocation(location.Id));
    Require(failedOrder.SetInReceiving(DateTimeOffset.UtcNow, user));
    Require(failedOrder.UpdateItemFactQuantity(1, 5m));
    setup.Add(failedOrder);
    await setup.SaveChangesAsync();
    string? oldFingerprint = failedOrder.SynchronizationFingerprint;
    source.Snapshot = failedSnapshot with { Status = ReceivingOrderStatus.InReceiving };
    sink.FailCompletion = true;
    var failedContext = new CommandContext(Guid.NewGuid(), user);
    ExpectError(await service.CompleteReceivingAsync(new(failedOrder.Id, otherLocation.Id), failedContext), OperationErrorType.Failure);
    await using (var verify = factory.CreateDbContext())
    {
        var saved = await verify.ReceivingOrders.SingleAsync(x => x.Id == failedOrder.Id);
        Check(saved.SynchronizationFingerprint != oldFingerprint, "Checkpoint independently saved");
        Check(saved.Status == ReceivingOrderStatus.InReceiving && saved.ReceivingLocationId == location.Id, "No final state leaked");
        Check(!await verify.InventoryMovements.AnyAsync(x => x.RecorderId == failedOrder.Id), "No movement leaked");
        Check(!await verify.CommandReceipts.AnyAsync(x => x.RequestId == failedContext.RequestId), "No success receipt on failure");
    }
    sink.FailCompletion = false;
    Require(await service.CompleteReceivingAsync(new(failedOrder.Id, otherLocation.Id), failedContext));
    Console.WriteLine("PASS: checkpoint survives final failure; retry succeeds with the same request.");

    // Two operations pass lookup before either commits. Distinct WMS inserts make
    // receipt uniqueness the decisive race; the loser's insert must roll back.
    var raceId = Guid.NewGuid();
    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    int arrived = 0;
    Task<OperationResult<Guid>> Race() => executor.ExecuteAsync("pilot.race", raceId, LegacyHash("race"), user,
        async (db, ct) =>
        {
            var effect = new Warehouse { Id = Guid.NewGuid(), Name = "race-effect" };
            db.Add(effect);
            if (Interlocked.Increment(ref arrived) == 2) ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
            return effect.Id;
        }, default);
    var raced = await Task.WhenAll(Race(), Race());
    Require(raced[0]); Require(raced[1]);
    Check(raced[0].Value == raced[1].Value, "Both callers receive winning resource id");
    await using (var verify = factory.CreateDbContext())
        Check(await verify.Warehouses.CountAsync(x => x.Name == "race-effect") == 1, "Losing transaction effect rolled back");

    var invalidRequest = Guid.NewGuid();
    try
    {
        await executor.ExecuteAsync("pilot.unknown-error", invalidRequest, LegacyHash("invalid"), user,
            (db, ct) =>
            {
                db.Warehouses.Add(new Warehouse { Id = warehouse.Id, Name = "duplicate primary key" });
                return Task.FromResult<OperationResult<Guid>>(warehouse.Id);
            }, default);
        throw new Exception("Unknown persistence error was swallowed");
    }
    catch (DbUpdateException)
    {
        await using var verify = factory.CreateDbContext();
        Check(!await verify.CommandReceipts.AnyAsync(x => x.RequestId == invalidRequest), "Failed save leaves no receipt");
    }
    Console.WriteLine("PASS: concurrent winner replay, atomic losing rollback, unknown persistence exception propagation.");

    ReceivingOrderImportSnapshot Snapshot() => new(
        Guid.NewGuid(), false, true, "Pilot", DateTime.UtcNow, warehouse.Id, null,
        ReceivingOrderStatus.ReadyForReceiving, default, WarehouseOperation.VendorReceipt,
        BusinessOperation.VendorPurchase, Guid.NewGuid(), default, Guid.NewGuid(), null,
        [new(1, sku.Id, 5m, 5m, null)]);
}
finally
{
    await setup.Database.EnsureDeletedAsync();
}

async Task InsertLegacyReceipt(string type, CommandContext context, Guid resourceId, string hash)
{
    await using var db = factory.CreateDbContext();
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MobileCommandReceipts (UserId, CommandType, ClientRequestId, RequestHash, ResultResourceId, CompletedAtUtc) VALUES ({context.UserId}, {type}, {context.RequestId}, {hash}, {resourceId}, {DateTimeOffset.UtcNow})");
}

static string LegacyHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void Require(OperationResult result) => Check(result.IsSuccess, result.Error?.Message ?? "Expected success");
static T Value<T>(OperationResult<T> result) { Require(result); return result.Value!; }
static void ExpectError(OperationResult result, OperationErrorType type) => Check(result.Error?.Type == type, $"Expected {type}, got {result.Error}");

sealed class ContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => new(options);
}

sealed class Source : IReceivingOrderSource
{
    public int Calls { get; private set; }
    public ReceivingOrderImportSnapshot? Snapshot { get; set; }
    public Task<OperationResult<ReceivingOrderImportSnapshot>> GetSnapshotAsync(Guid orderId, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult<OperationResult<ReceivingOrderImportSnapshot>>(Snapshot ?? throw new Exception("Unexpected 1C access"));
    }
}

sealed class Sink : IReceivingOrderExecutionSink
{
    public int Calls { get; private set; }
    public bool FailCompletion { get; set; }
    public Func<Task>? StartGate { get; set; }
    public async Task<OperationResult> SetInReceivingAsync(Guid orderId, CancellationToken ct)
    {
        Calls++;
        if (StartGate is { } gate) await gate();
        return OperationResult.Success();
    }
    public Task<OperationResult> UpdateItemsAsync(Guid orderId, IReadOnlyCollection<ReceivingOrderItem> items, CancellationToken ct) { Calls++; return Task.FromResult(OperationResult.Success()); }
    public Task<OperationResult> SetReceivedAsync(Guid orderId, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(FailCompletion ? OperationResult.Failure(OperationError.Failure("Target unavailable")) : OperationResult.Success());
    }
}
