using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Counts;
using Wms.Application.Inventory.Movements;
using Wms.Application.StockKeepingUnits;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

// No application connection string: only this uniquely named disposable database.
var database = "WmsCountCommands_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
InventoryCountCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), new InventoryPostingService(NullLogger<InventoryPostingService>.Instance), new StockKeepingUnitService(f));
InventoryCountCommandService RacingService() => Service(new ContextFactory(
    new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "count-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid(); string n = missing.ToString("N");
    var contexts = Enumerable.Range(0, 7).Select(_ => Context()).ToArray();
    string[] types = ["create", "increment-sku", "set-quantity", "set-sku-quantity", "remove-item", "post", "delete-draft"];
    string[] inputs = [$"{n}|{n}", $"{n}| RAW ", $"{n}|{n}|1.25", $"{n}|{n}|1.25", $"{n}|{n}", n, n];
    for (int i = 0; i < contexts.Length; i++)
        setup.CommandReceipts.Add(new CommandReceipt { UserId = contexts[i].UserId, RequestId = contexts[i].RequestId,
            CommandType = "inventory-count." + types[i], RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputs[i]))),
            ResultResourceId = missing, CompletedAtUtc = DateTimeOffset.UtcNow });
    await setup.SaveChangesAsync();
    Check(Value(await commands.StartAsync(new(missing, missing), contexts[0])) == missing, "Legacy start");
    Check(Value(await commands.IncrementSkuAsync(new(missing, " RAW "), contexts[1])) == missing, "Raw barcode replay before resolution");
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Value(await commands.SetCountedQuantityAsync(new(missing, missing, 1.250m), contexts[2]));
    Value(await commands.SetSkuCountedQuantityAsync(new(missing, missing, 1.250m), contexts[3]));
    Value(await commands.RemoveUnexpectedItemAsync(new(missing, missing), contexts[4]));
    Value(await commands.PostAsync(missing, contexts[5])); Value(await commands.DeleteDraftAsync(missing, contexts[6]));
    Expect(await commands.IncrementSkuAsync(new(missing, "RAW"), contexts[1]), OperationErrorType.Conflict);
    Expect(await commands.SetCountedQuantityAsync(new(missing, missing, 2m), contexts[2]), OperationErrorType.Conflict);
    Expect(await commands.StartAsync(new(missing, missing), new(Guid.Empty, "count-test")), OperationErrorType.Invalid);
    Expect(await commands.PostAsync(missing, new(Guid.NewGuid(), "")), OperationErrorType.Invalid);
    Console.WriteLine("PASS: seven legacy receipts, original barcode/decimal hashes, changed input and caller validation.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Count test" };
    var expectedSku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "Expected" };
    var unexpectedSku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "Unexpected" };
    var zone = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "S", "Storage", ZoneType.Storage));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    StorageLocation Location(int number) => Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, number, number.ToString(), details));
    var location = Location(1); var raceLocation = Location(2);
    setup.AddRange(warehouse, expectedSku, unexpectedSku, zone, location, raceLocation,
        new SkuBarcode { SkuId = expectedSku.Id, Value = "EXPECTED" },
        new SkuBarcode { SkuId = unexpectedSku.Id, Value = "UNEXPECTED" },
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, location.Id, expectedSku.Id, 10m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    var startContext = Context(); var start = new StartInventoryCountCommand(warehouse.Id, location.Id);
    var countId = Value(await commands.StartAsync(start, startContext));
    Check(Value(await commands.StartAsync(start, startContext)) == countId, "Start replay");
    Check(Value(await commands.StartAsync(start, Context())) == countId, "Start resumes existing draft");
    Expect(await commands.StartAsync(start with { WarehouseId = Guid.NewGuid() }, Context()), OperationErrorType.Invalid);
    var draft = await Load(countId); var itemId = draft.Items.Single().Id;
    Check(draft.Items.Single().ExpectedQuantity == 10m && draft.Items.Single().CountedQuantity is null, "Snapshot starts uncounted");
    await Verify(countId, location.Id, InventoryCountStatus.Draft, true, 0, 0);
    var postContext = Context(); Expect(await commands.PostAsync(countId, postContext), OperationErrorType.Invalid);
    await NoReceipt(postContext);
    var scanContext = Context(); var scan = new IncrementInventoryCountSkuCommand(countId, "EXPECTED");
    Check(Value(await commands.IncrementSkuAsync(scan, scanContext)) == itemId, "Expected scan result");
    Value(await commands.IncrementSkuAsync(scan, scanContext));
    Check((await Load(countId)).Items.Single().CountedQuantity == 1m, "Scan increments once");
    // Remove mutable barcode mapping: completed attempt must still replay.
    await using (var db = factory.CreateDbContext())
    {
        db.SkuBarcodes.Remove(await db.SkuBarcodes.SingleAsync(x => x.Value == "EXPECTED"));
        await db.SaveChangesAsync();
    }
    Value(await commands.IncrementSkuAsync(scan, scanContext));
    Expect(await commands.RemoveUnexpectedItemAsync(new(countId, itemId), Context()), OperationErrorType.Invalid);
    var invalidContext = Context();
    Expect(await commands.SetCountedQuantityAsync(new(countId, itemId, -1m), invalidContext), OperationErrorType.Invalid);
    await NoReceipt(invalidContext);
    Value(await commands.SetCountedQuantityAsync(new(countId, itemId, 8.5m), Context()));
    var unexpected = Value(await commands.IncrementSkuAsync(new(countId, "UNEXPECTED"), Context()));
    var removalContext = Context();
    Value(await commands.RemoveUnexpectedItemAsync(new(countId, unexpected), removalContext));
    Value(await commands.RemoveUnexpectedItemAsync(new(countId, unexpected), removalContext));
    var setContext = Context(); var set = new SetInventoryCountSkuQuantityCommand(countId, unexpectedSku.Id, 2.25m);
    var added = Value(await commands.SetSkuCountedQuantityAsync(set, setContext));
    Check(Value(await commands.SetSkuCountedQuantityAsync(set with { CountedQuantity = 2.250m }, setContext)) == added, "Absolute SKU replay");
    Value(await commands.PostAsync(countId, postContext)); Value(await commands.PostAsync(countId, postContext));
    await Verify(countId, location.Id, InventoryCountStatus.Posted, false, 2, 2);
    await using (var db = factory.CreateDbContext())
    {
        Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == location.Id && x.StockKeepingUnitId == expectedSku.Id)).Quantity == 8.5m, "Negative difference posted once");
        Check((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == location.Id && x.StockKeepingUnitId == unexpectedSku.Id)).Quantity == 2.25m, "Positive difference posted once");
    }
    Expect(await commands.SetCountedQuantityAsync(new(countId, itemId, 0m), Context()), OperationErrorType.Invalid);
    Expect(await commands.DeleteDraftAsync(countId, Context()), OperationErrorType.Invalid);
    var deleteId = Value(await commands.StartAsync(start, Context())); var deleteContext = Context();
    Value(await commands.DeleteDraftAsync(deleteId, deleteContext)); Value(await commands.DeleteDraftAsync(deleteId, deleteContext));
    await using (var db = factory.CreateDbContext())
        Check(!await db.StorageLocationLocks.AnyAsync(x => x.StorageLocationId == location.Id) && !await db.InventoryCounts.AnyAsync(x => x.Id == deleteId), "Deletion releases lock and removes draft");
    Console.WriteLine("PASS: snapshot and lock, scan once, absolute quantities, removal/deletion replay, rejected then successful posting and differences.");

    var shared = Context(); var raceStart = new StartInventoryCountCommand(warehouse.Id, raceLocation.Id);
    var concurrent = RacingService();
    var sameStart = await Task.WhenAll(concurrent.StartAsync(raceStart, shared), concurrent.StartAsync(raceStart, shared));
    var raceId = Value(sameStart[0]); Check(raceId == Value(sameStart[1]), "Concurrent start winning receipt");
    Value(await commands.DeleteDraftAsync(raceId, Context()));
    concurrent = RacingService();
    var distinctStart = await Task.WhenAll(concurrent.StartAsync(raceStart, Context()), concurrent.StartAsync(raceStart, Context()));
    Check(distinctStart.Count(x => x.IsSuccess) == 1, "One lock acquisition");
    Expect(distinctStart.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    raceId = Value(distinctStart.Single(x => x.IsSuccess));
    var raceScan = new IncrementInventoryCountSkuCommand(raceId, "UNEXPECTED"); shared = Context(); concurrent = RacingService();
    var sameScan = await Task.WhenAll(concurrent.IncrementSkuAsync(raceScan, shared), concurrent.IncrementSkuAsync(raceScan, shared));
    Check(Value(sameScan[0]) == Value(sameScan[1]), "Concurrent scan winning receipt");
    Check((await Load(raceId)).Items.Single().CountedQuantity == 1m, "Duplicate scan increments once");
    concurrent = RacingService();
    var distinctScan = await Task.WhenAll(concurrent.IncrementSkuAsync(raceScan, Context()), concurrent.IncrementSkuAsync(raceScan, Context()));
    Check(distinctScan.Count(x => x.IsSuccess) == 1, "One concurrent edit succeeds");
    Expect(distinctScan.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    Check((await Load(raceId)).Items.Single().CountedQuantity == 2m, "No lost/double increment");
    shared = Context(); concurrent = RacingService();
    var samePost = await Task.WhenAll(concurrent.PostAsync(raceId, shared), concurrent.PostAsync(raceId, shared));
    Check(Value(samePost[0]) == Value(samePost[1]), "Concurrent post winning receipt");
    await Verify(raceId, raceLocation.Id, InventoryCountStatus.Posted, false, 1, 1);
    // Zero is counted; no-difference posting still releases the lock.
    var zeroId = Value(await commands.StartAsync(raceStart, Context()));
    Value(await commands.SetSkuCountedQuantityAsync(new(zeroId, unexpectedSku.Id, 2m), Context()));
    Value(await commands.SetSkuCountedQuantityAsync(new(zeroId, expectedSku.Id, 0m), Context()));
    Value(await commands.PostAsync(zeroId, Context()));
    await Verify(zeroId, raceLocation.Id, InventoryCountStatus.Posted, false, 0, 0);
    Console.WriteLine("PASS: deterministic concurrent start/scan/post, distinct conflicts, zero/no-difference posting and lock release.");

    var staleId = Value(await commands.StartAsync(raceStart, Context()));
    Value(await commands.SetSkuCountedQuantityAsync(new(staleId, unexpectedSku.Id, 1m), Context()));
    // Simulate out-of-band stock drift while the draft holds its location lock.
    await Adjust(1m);
    var stalePost = Context(); Expect(await commands.PostAsync(staleId, stalePost), OperationErrorType.Conflict);
    await NoReceipt(stalePost); await Verify(staleId, raceLocation.Id, InventoryCountStatus.Draft, true, 0, 0);
    await Adjust(-1m);
    Value(await commands.PostAsync(staleId, stalePost));
    await Verify(staleId, raceLocation.Id, InventoryCountStatus.Posted, false, 1, 1);
    await using (var db = factory.CreateDbContext())
    {
        db.StorageLocationLocks.Add(Value(StorageLocationLock.CreateManual(raceLocation.Id, "Test lock", DateTimeOffset.UtcNow, "count-test")));
        await db.SaveChangesAsync();
    }
    var blockedStart = Context(); Expect(await commands.StartAsync(raceStart, blockedStart), OperationErrorType.Conflict);
    await NoReceipt(blockedStart);
    Console.WriteLine("PASS: stock drift blocks posting without effects; same attempt succeeds after recovery; manual lock blocks start.");

    async Task Adjust(decimal delta)
    {
        await using var db = factory.CreateDbContext();
        Value((await db.InventoryBalances.SingleAsync(x => x.StorageLocationId == raceLocation.Id && x.StockKeepingUnitId == unexpectedSku.Id)).Adjust(delta, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }

    async Task<InventoryCount> Load(Guid id) { await using var db = factory.CreateDbContext(); return await db.InventoryCounts.Include(x => x.Items).SingleAsync(x => x.Id == id); }
    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
    async Task Verify(Guid id, Guid locationId, InventoryCountStatus status, bool locked, int movements, int turnovers)
    {
        await using var db = factory.CreateDbContext();
        Check((await db.InventoryCounts.SingleAsync(x => x.Id == id)).Status == status, "Status");
        Check(await db.StorageLocationLocks.AnyAsync(x => x.StorageLocationId == locationId && x.OwnerId == id) == locked, "Lock ownership");
        Check(await db.InventoryMovements.CountAsync(x => x.RecorderId == id && x.PostedAtUtc != null) == movements, "Posted count");
        Check(await db.InventoryTurnovers.CountAsync(x => x.InventoryMovement!.RecorderId == id) == turnovers, "Turnover count");
    }
}
finally { await setup.Database.EnsureDeletedAsync(); }

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
