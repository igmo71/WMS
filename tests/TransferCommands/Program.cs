using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.Application.Commands;
using Wms.Application.Inventory.Movements;
using Wms.Application.Inventory.Transfers;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

var database = "WmsTransferCommands_" + Guid.NewGuid().ToString("N");
using var services = new ServiceCollection().Configure<IdentityOptions>(o =>
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseApplicationServiceProvider(services)
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
var factory = new ContextFactory(options);
InventoryTransferCommandService Service(IDbContextFactory<ApplicationDbContext> f) =>
    new(new CommandExecutor(f), new InventoryPostingService(NullLogger<InventoryPostingService>.Instance));
var commands = Service(factory);
CommandContext Context() => new(Guid.NewGuid(), "transfer-test");
await using var setup = factory.CreateDbContext();
try
{
    Check(!setup.Database.HasPendingModelChanges(), "No model drift");
    await setup.Database.MigrateAsync();
    var missing = Guid.NewGuid();
    var contexts = Enumerable.Range(0, 6).Select(_ => Context()).ToArray();
    string[] types = ["create-draft", "create-draft", "move-direct", "pick-to-transit", "put-from-transit", "complete"];
    string n = missing.ToString("N");
    string[] inputs = [n, $"{n}|{n}", $"{n}|{n}|{n}|{n}|1.25", $"{n}|{n}|{n}|1.25", $"{n}|{n}|{n}|1.25", n];
    for (int i = 0; i < contexts.Length; i++)
        setup.CommandReceipts.Add(new CommandReceipt { UserId = contexts[i].UserId, RequestId = contexts[i].RequestId,
            CommandType = "inventory-transfer." + types[i], RequestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputs[i]))),
            ResultResourceId = missing, CompletedAtUtc = DateTimeOffset.UtcNow });
    await setup.SaveChangesAsync();
    Check(Value(await commands.CreateAsync(new(missing, null), contexts[0])) == missing, "Old direct create");
    Check(Value(await commands.CreateAsync(new(missing, missing), contexts[1])) == missing, "Old transit create");
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Check(Value(await commands.MoveDirectAsync(new(missing, missing, missing, missing, 1.250m), contexts[2])) == missing, "Old direct hash");
    Value(await commands.PickAsync(new(missing, missing, missing, 1.250m), contexts[3]));
    Value(await commands.PutAsync(new(missing, missing, missing, 1.250m), contexts[4]));
    Value(await commands.CompleteAsync(missing, contexts[5]));
    Expect(await commands.MoveDirectAsync(new(missing, missing, missing, missing, 2m), contexts[2]), OperationErrorType.Conflict);
    Console.WriteLine("PASS: legacy receipt lookup precedes mutable state; invariant hashes and input conflict.");

    var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Transfer test" };
    var sku = new StockKeepingUnit { Id = Guid.NewGuid(), Name = "SKU" };
    var storage = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "S", "Storage", ZoneType.Storage));
    var transit = Value(Zone.Create(Guid.NewGuid(), warehouse.Id, "T", "Transit", ZoneType.Transit));
    var details = Value(StorageLocationDetails.Create("Location", false, LocationDimensions.Empty, LocationCoordinates.Empty, null));
    StorageLocation Location(Zone zone, int number) => Value(StorageLocation.Create(Guid.NewGuid(), warehouse.Id, zone.Id, null, number, number.ToString(), details));
    var source = Location(storage, 1); var destination = Location(storage, 2); var cart = Location(transit, 1);
    setup.AddRange(warehouse, sku, storage, transit, source, destination, cart,
        Value(InventoryBalance.Create(Guid.NewGuid(), warehouse.Id, source.Id, sku.Id, 100m, DateTimeOffset.UtcNow)));
    await setup.SaveChangesAsync();
    var direct = Value(await commands.CreateAsync(new(warehouse.Id, null), Context()));
    var move = new MoveDirectInventoryTransferCommand(direct, source.Id, destination.Id, sku.Id, 5.125m);
    var moveContext = Context();
    var movement = Value(await commands.MoveDirectAsync(move, moveContext));
    Check(Value(await commands.MoveDirectAsync(move with { Quantity = 5.1250m }, moveContext)) == movement, "Direct replay");
    await Verify(direct, 1, 2);
    Expect(await commands.DeleteDraftAsync(direct, Context()), OperationErrorType.Invalid);
    var finish = Context(); Value(await commands.CompleteAsync(direct, finish)); Value(await commands.CompleteAsync(direct, finish));
    Expect(await commands.MoveDirectAsync(move, Context()), OperationErrorType.Invalid);
    var transfer = Value(await commands.CreateAsync(new(warehouse.Id, cart.Id), Context()));
    Check(!(await commands.CreateAsync(new(warehouse.Id, cart.Id), Context())).IsSuccess, "Exclusive transit");
    var pick = new PickInventoryTransferCommand(transfer, source.Id, sku.Id, 3m); var pickContext = Context();
    var picked = Value(await commands.PickAsync(pick, pickContext));
    Check(Value(await commands.PickAsync(pick, pickContext)) == picked, "Pick replay");
    var completeContext = Context(); Expect(await commands.CompleteAsync(transfer, completeContext), OperationErrorType.Invalid);
    await NoReceipt(completeContext);
    var put = new PutInventoryTransferCommand(transfer, destination.Id, sku.Id, 3m); var putContext = Context();
    var placed = Value(await commands.PutAsync(put, putContext)); Check(Value(await commands.PutAsync(put, putContext)) == placed, "Put replay");
    Value(await commands.CompleteAsync(transfer, completeContext)); await Verify(transfer, 2, 4);
    var draft = Value(await commands.CreateAsync(new(warehouse.Id, cart.Id), Context()));
    var deletion = Context(); Value(await commands.DeleteDraftAsync(draft, deletion)); Value(await commands.DeleteDraftAsync(draft, deletion));
    Expect(await commands.DeleteDraftAsync(draft, Context()), OperationErrorType.NotFound);
    var retryTransfer = Value(await commands.CreateAsync(new(warehouse.Id, null), Context()));
    var insufficient = new MoveDirectInventoryTransferCommand(retryTransfer, destination.Id, source.Id, sku.Id, 9m);
    var retryContext = Context(); Check(!(await commands.MoveDirectAsync(insufficient, retryContext)).IsSuccess, "Insufficient stock");
    await NoReceipt(retryContext); await Verify(retryTransfer, 0, 0);
    Value(await commands.MoveDirectAsync(new(retryTransfer, source.Id, destination.Id, sku.Id, 1m), Context()));
    Value(await commands.MoveDirectAsync(insufficient, retryContext)); await Verify(retryTransfer, 2, 4);
    Console.WriteLine("PASS: direct/transit posting, replay, draft deletion, rejection without effects and retry.");

    // Force both requests to finish validation before either final save starts.
    var shared = Context();
    var concurrent = Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    var same = await Task.WhenAll(concurrent.CreateAsync(new(warehouse.Id, cart.Id), shared), concurrent.CreateAsync(new(warehouse.Id, cart.Id), shared));
    Check(Value(same[0]) == Value(same[1]), "Winning receipt for concurrent create");
    Value(await commands.DeleteDraftAsync(Value(same[0]), Context()));
    concurrent = Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    var distinct = await Task.WhenAll(concurrent.CreateAsync(new(warehouse.Id, cart.Id), Context()), concurrent.CreateAsync(new(warehouse.Id, cart.Id), Context()));
    Check(distinct.Count(x => x.IsSuccess) == 1, "One transit owner");
    Expect(distinct.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    var repeatedMove = new MoveDirectInventoryTransferCommand(retryTransfer, source.Id, destination.Id, sku.Id, 1m);
    shared = Context();
    concurrent = Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    var repeated = await Task.WhenAll(concurrent.MoveDirectAsync(repeatedMove, shared), concurrent.MoveDirectAsync(repeatedMove, shared));
    Check(Value(repeated[0]) == Value(repeated[1]), "Winning movement receipt"); await Verify(retryTransfer, 3, 6);
    var otherTransfer = Value(await commands.CreateAsync(new(warehouse.Id, null), Context()));
    concurrent = Service(new ContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>(options).AddInterceptors(new SaveBarrier()).Options));
    var stockRace = await Task.WhenAll(
        concurrent.MoveDirectAsync(new(retryTransfer, destination.Id, source.Id, sku.Id, 0.75m), Context()),
        concurrent.MoveDirectAsync(new(otherTransfer, destination.Id, source.Id, sku.Id, 0.75m), Context()));
    Check(stockRace.Count(x => x.IsSuccess) == 1, "Only one competing stock withdrawal succeeds");
    Expect(stockRace.Single(x => !x.IsSuccess), OperationErrorType.Conflict);
    await using var check = factory.CreateDbContext();
    Check(await check.InventoryBalances.SumAsync(x => x.Quantity) == 100m, "Stock conserved");
    Check(!await check.InventoryBalances.AnyAsync(x => x.Quantity < 0), "No negative stock");
    Console.WriteLine("PASS: simultaneous duplicate requests and conflicting transit acquisition; conserved stock.");

    async Task NoReceipt(CommandContext c) { await using var db = factory.CreateDbContext(); Check(!await db.CommandReceipts.AnyAsync(x => x.RequestId == c.RequestId), "Rejected request has no receipt"); }
    async Task Verify(Guid id, int movements, int turnovers)
    {
        await using var db = factory.CreateDbContext();
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
