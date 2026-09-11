using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.Application.Inventory.Movements;
using Wms.Application.Commands;
using System.Globalization;
using Wms.Application.StorageLocations;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.ReceivingOrders;

public class PutawayCommandService(
    CommandExecutor commandExecutor,
    InventoryPostingService inventoryPostingService,
    ILogger<PutawayCommandService> logger)
{
    private const string StartCommandType = "receiving-order.start-putaway";
    private const string AddCommandType = "receiving-order.add-putaway-movement";
    private const string UpdateCommandType = "receiving-order.update-putaway-movement";
    private const string DeleteCommandType = "receiving-order.delete-putaway-movement";
    private const string CompleteCommandType = "receiving-order.complete-putaway";

    public Task<OperationResult<Guid>> StartAsync(Guid orderId, CommandContext context, CancellationToken ct = default) =>
        ExecuteActionAsync(StartCommandType, orderId, context, CommandExecutor.ComputeHash(orderId.ToString("N")),
            (db, token) => StartCoreAsync(db, orderId, context.UserId, token), ct);

    public Task<OperationResult<Guid>> AddMovementAsync(AddPutawayMovementCommand command, CommandContext context, CancellationToken ct = default) =>
        commandExecutor.ExecuteAsync(AddCommandType, context.RequestId,
            CommandExecutor.ComputeHash($"{command.OrderId:N}|{command.LineNumber.ToString(CultureInfo.InvariantCulture)}|{command.DestinationStorageLocationId:N}|{command.Quantity.ToString("G29", CultureInfo.InvariantCulture)}"),
            context.UserId,
            async (db, token) =>
            {
                var result = await AddMovementCoreAsync(db, command.OrderId, command.LineNumber, command.DestinationStorageLocationId, command.Quantity, token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            }, ct);

    public Task<OperationResult<Guid>> UpdateMovementAsync(UpdatePutawayMovementCommand command, CommandContext context, CancellationToken ct = default) =>
        ExecuteActionAsync(UpdateCommandType, command.MovementId, context,
            CommandExecutor.ComputeHash($"{command.OrderId:N}|{command.MovementId:N}|{command.DestinationStorageLocationId:N}|{command.Quantity.ToString("G29", CultureInfo.InvariantCulture)}"),
            (db, token) => UpdateMovementCoreAsync(db, command.OrderId, command.MovementId, command.DestinationStorageLocationId, command.Quantity, token), ct);

    public Task<OperationResult<Guid>> DeleteMovementAsync(DeletePutawayMovementCommand command, CommandContext context, CancellationToken ct = default) =>
        ExecuteActionAsync(DeleteCommandType, command.MovementId, context,
            CommandExecutor.ComputeHash($"{command.OrderId:N}|{command.MovementId:N}"),
            (db, token) => DeleteMovementCoreAsync(db, command.OrderId, command.MovementId, token), ct);

    public Task<OperationResult<Guid>> CompleteAsync(Guid orderId, CommandContext context, CancellationToken ct = default) =>
        ExecuteActionAsync(CompleteCommandType, orderId, context, CommandExecutor.ComputeHash(orderId.ToString("N")),
            (db, token) => CompleteCoreAsync(db, orderId, context.UserId, token), ct);

    private Task<OperationResult<Guid>> ExecuteActionAsync(string type, Guid resourceId, CommandContext context, string hash,
        Func<ApplicationDbContext, CancellationToken, Task<OperationResult>> action, CancellationToken ct) =>
        commandExecutor.ExecuteAsync(type, context.RequestId, hash, context.UserId,
            async (db, token) =>
            {
                var result = await action(db, token);
                return result.IsSuccess ? resourceId : result.Error!;
            }, ct);

    private async Task<OperationResult> StartCoreAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        string userId,
        CancellationToken ct)
    {
        using var scope = logger.BeginScope("Putaway Start {OrderId}", orderId);

        var order = await LoadEditableOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        var startResult = order.StartPutaway(DateTimeOffset.UtcNow, userId);
        if (!startResult.IsSuccess)
        {
            return startResult;
        }

        return startResult;
    }

    private async Task<OperationResult<InventoryMovement>> AddMovementCoreAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        int lineNumber,
        Guid destinationStorageLocationId,
        decimal quantity,
        CancellationToken ct)
    {
        var order = await LoadEditableOrderAsync(dbContext, orderId, ct);

        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        var draftMovements = await LoadDraftMovementsAsync(dbContext, order.Id, ct);
        var movementResult = order.CreatePutawayMovement(
            Guid.NewGuid(),
            lineNumber,
            destinationStorageLocationId,
            quantity,
            DateTimeOffset.UtcNow,
            draftMovements);
        if (!movementResult.IsSuccess)
        {
            return movementResult.Error!;
        }

        var movement = movementResult.Value!;
        var destinationResult = await ValidateDestinationAsync(
            dbContext, order, destinationStorageLocationId, ct);
        if (!destinationResult.IsSuccess)
        {
            return destinationResult.Error!;
        }

        var balanceResult = await ValidateSourceBalanceAsync(
            dbContext, order, movement, draftMovements, null, ct);
        if (!balanceResult.IsSuccess)
        {
            return balanceResult.Error!;
        }

        dbContext.InventoryMovements.Add(movement);
        return movement;
    }

    private async Task<OperationResult> UpdateMovementCoreAsync(
        ApplicationDbContext dbContext,
        Guid expectedOrderId,
        Guid movementId,
        Guid destinationStorageLocationId,
        decimal quantity,
        CancellationToken ct)
    {
        var movement = await dbContext.InventoryMovements
            .FirstOrDefaultAsync(x => x.Id == movementId, ct);

        if (movement is null)
        {
            return OperationError.NotFound($"Движение размещения '{movementId}' не найдено.");
        }

        if (movement.RecorderId != expectedOrderId)
            return OperationError.NotFound($"Движение размещения '{movementId}' не найдено в приходном ордере '{expectedOrderId}'.");

        var order = movement.RecorderId is Guid orderId
            ? await LoadEditableOrderAsync(dbContext, orderId, ct)
            : null;
        if (order is null)
        {
            return OperationError.NotFound(
                $"Приходный ордер '{movement.RecorderId}' для движения размещения '{movementId}' не найден.");
        }

        var draftMovements = await LoadDraftMovementsAsync(dbContext, order.Id, ct);
        var updateResult = order.UpdatePutawayMovement(
            movement,
            destinationStorageLocationId,
            quantity,
            DateTimeOffset.UtcNow,
            draftMovements);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        var destinationResult = await ValidateDestinationAsync(
            dbContext, order, destinationStorageLocationId, ct);
        if (!destinationResult.IsSuccess)
        {
            return destinationResult;
        }

        var balanceResult = await ValidateSourceBalanceAsync(
            dbContext, order, movement, draftMovements, movement.Id, ct);
        if (!balanceResult.IsSuccess)
        {
            return balanceResult;
        }

        return OperationResult.Success();
    }

    private static async Task<OperationResult> DeleteMovementCoreAsync(
        ApplicationDbContext dbContext,
        Guid expectedOrderId,
        Guid movementId,
        CancellationToken ct)
    {
        var movement = await dbContext.InventoryMovements
            .FirstOrDefaultAsync(x => x.Id == movementId, ct);

        if (movement is null)
        {
            return OperationError.NotFound($"Движение размещения '{movementId}' не найдено.");
        }

        if (movement.RecorderId != expectedOrderId)
        {
            return OperationError.NotFound(
                $"Движение размещения '{movementId}' не найдено в приходном ордере '{expectedOrderId}'.");
        }

        var order = movement.RecorderId is Guid recorderOrderId
            ? await dbContext.ReceivingOrders.FirstOrDefaultAsync(x => x.Id == recorderOrderId, ct)
            : null;

        if (order is null)
        {
            return OperationError.NotFound(
                $"Приходный ордер '{movement.RecorderId}' для движения размещения '{movementId}' не найден.");
        }

        var removalResult = order.RemovePutawayMovement(movement);
        if (!removalResult.IsSuccess)
        {
            return removalResult;
        }

        dbContext.InventoryMovements.Remove(movement);
        return OperationResult.Success();
    }

    private async Task<OperationResult> CompleteCoreAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        string userId,
        CancellationToken ct)
    {
        using var scope = logger.BeginScope("Putaway Complete {OrderId}", orderId);

        var order = await LoadEditableOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        var draftMovements = await LoadDraftMovementsAsync(dbContext, order.Id, ct);
        var completionResult = order.CompletePutaway(draftMovements, DateTimeOffset.UtcNow, userId);
        if (!completionResult.IsSuccess)
        {
            return completionResult;
        }

        var routesValidation = await ReceivingOrderLocationPolicy.ValidatePutawayRoutesAsync(
            dbContext, order, draftMovements, ct);
        if (!routesValidation.IsSuccess)
        {
            return routesValidation;
        }

        foreach (var movement in draftMovements)
        {
            var confirmationResult = movement.Confirm(userId);
            if (!confirmationResult.IsSuccess)
            {
                return confirmationResult;
            }
        }

        var postingResult = await inventoryPostingService
            .PostInventoryMovementsAsync(draftMovements, dbContext, ct);
        if (!postingResult.IsSuccess)
        {
            return postingResult;
        }

        return OperationResult.Success();
    }

    private static Task<ReceivingOrder?> LoadEditableOrderAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        CancellationToken ct) =>
        dbContext.ReceivingOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    private static Task<List<InventoryMovement>> LoadDraftMovementsAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        CancellationToken ct) =>
        dbContext.InventoryMovements
            .Where(x => x.PostedAtUtc == null
                && x.RecorderType == RecorderType.ReceivingOrder
                && x.RecorderId == orderId)
            .ToListAsync(ct);

    private static async Task<OperationResult> ValidateDestinationAsync(
        ApplicationDbContext dbContext,
        ReceivingOrder order,
        Guid destinationStorageLocationId,
        CancellationToken ct)
    {
        if (destinationStorageLocationId == order.ReceivingLocationId)
        {
            return OperationError.Invalid("Позиция назначения должна отличаться от позиции приёмки.");
        }

        var destinationResult = await ReceivingOrderLocationPolicy.RequirePutawayDestinationAsync(
            dbContext,
            order,
            destinationStorageLocationId,
            ct);
        if (!destinationResult.IsSuccess)
        {
            return destinationResult;
        }

        var source = await dbContext.StorageLocations
            .Include(x => x.Zone)
            .Include(x => x.ActiveLock)
            .SingleAsync(x => x.Id == order.ReceivingLocationId, ct);
        return StorageLocationAvailability.ValidateUnlocked(source);
    }

    private static async Task<OperationResult> ValidateSourceBalanceAsync(
        ApplicationDbContext dbContext,
        ReceivingOrder order,
        InventoryMovement movement,
        IReadOnlyCollection<InventoryMovement> draftMovements,
        Guid? excludedMovementId,
        CancellationToken ct)
    {
        var sourceBalance = await dbContext.InventoryBalances
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.WarehouseId == order.WarehouseId
                && x.StorageLocationId == order.ReceivingLocationId
                && x.StockKeepingUnitId == movement.StockKeepingUnitId, ct);

        var skuQuantity = draftMovements
            .Where(x => x.Id != excludedMovementId
                && x.StockKeepingUnitId == movement.StockKeepingUnitId)
            .Sum(x => x.Quantity) + movement.Quantity;

        if (sourceBalance is null || skuQuantity > sourceBalance.Quantity)
        {
            return OperationError.Invalid(
                "Количество размещения превышает доступный остаток в позиции приёмки.");
        }

        return OperationResult.Success();
    }

}
