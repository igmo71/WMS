using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.Application.Inventory.Movements;
using Wms.Application.Persistence;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.ReceivingOrders;

public class ReceivingOrderCommandService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    InventoryPostingService inventoryPostingService,
    ReceivingOrderSynchronizationService synchronizationService,
    IReceivingOrderExecutionSink executionSink,
    ILogger<ReceivingOrderCommandService> logger)
{
    public async Task<OperationResult> StartReceivingAsync(
        Guid orderId,
        Guid receivingLocationId,
        string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        OperationResult result = await StageStartReceivingAsync(
            dbContext,
            orderId,
            receivingLocationId,
            userId,
            ct);

        return result.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : result;
    }

    internal async Task<OperationResult> StageStartReceivingAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        Guid receivingLocationId,
        string userId,
        CancellationToken ct)
    {
        using var scope = logger.BeginScope("ReceivingOrder Start {OrderId}", orderId);
        using var activity = AppTracing.StartActivity(
            "ReceivingOrder.Start",
            nameof(ReceivingOrderCommandService));

        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            logger.LogError("Приходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        OperationResult synchronizationResult = EnsureSynchronizationAllowsWork(order);
        if (!synchronizationResult.IsSuccess)
            return synchronizationResult;

        var locationResult = await StageSetReceivingLocationAsync(
            dbContext,
            order,
            receivingLocationId,
            ct);
        if (!locationResult.IsSuccess)
        {
            return locationResult;
        }

        return await StageSetInReceivingAsync(order, userId, ct);
    }

    private async Task<OperationResult> StageSetInReceivingAsync(
        ReceivingOrder order,
        string userId,
        CancellationToken ct)
    {
        var transitionResult = order.SetInReceiving(DateTimeOffset.UtcNow, userId);
        if (!transitionResult.IsSuccess)
        {
            logger.LogError("Не удалось перевести приходный ордер в приемку: {ErrorMessage}", transitionResult.Error?.Message);
            return transitionResult;
        }

        var externalResult = await executionSink.SetInReceivingAsync(order.Id, ct);

        if (!externalResult.IsSuccess)
        {
            logger.LogError("Не удалось перевести документ 1С в приемку: {ErrorMessage}", externalResult.Error?.Message);
            return externalResult;
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> CompleteReceivingAsync(
        Guid orderId,
        Guid receivingLocationId,
        string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        OperationResult result = await ExecuteCompletionWithSynchronizationCheckpointAsync(
            dbContext,
            orderId,
            receivingLocationId,
            userId,
            ct);
        return result.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : result;
    }

    internal Task<OperationResult> ExecuteCompletionWithSynchronizationCheckpointAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        string userId,
        CancellationToken ct) =>
        ExecuteCompletionWithSynchronizationCheckpointAsync(
            dbContext,
            orderId,
            receivingLocationId: null,
            userId,
            ct);

    private async Task<OperationResult> ExecuteCompletionWithSynchronizationCheckpointAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        Guid? receivingLocationId,
        string userId,
        CancellationToken ct)
    {
        using var scope = logger.BeginScope("ReceivingOrder SetReceived {OrderId}", orderId);
        using var activity = AppTracing.StartActivity(
            "ReceivingOrder.SetReceived",
            nameof(ReceivingOrderCommandService));

        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            logger.LogError("Приходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        OperationResult synchronizationResult = await synchronizationService.PersistCompletionCheckpointAsync(
            dbContext,
            order,
            ct);
        if (!synchronizationResult.IsSuccess)
            return synchronizationResult;

        return await StageSetReceivedAsync(dbContext, order, receivingLocationId, userId, ct);
    }

    private async Task<OperationResult> StageSetReceivedAsync(
        ApplicationDbContext dbContext,
        ReceivingOrder order,
        Guid? receivingLocationId,
        string userId,
        CancellationToken ct)
    {
        if (receivingLocationId is Guid selectedLocationId)
        {
            var setLocationResult = await StageSetReceivingLocationAsync(
                dbContext,
                order,
                selectedLocationId,
                ct);
            if (!setLocationResult.IsSuccess)
                return setLocationResult;
        }

        var locationResult = await ReceivingOrderLocationPolicy.RequireReceivingLocationAsync(
            dbContext,
            order,
            order.ReceivingLocationId,
            ct);
        if (!locationResult.IsSuccess)
            return locationResult;

        var now = DateTimeOffset.UtcNow;
        var transitionResult = order.SetReceived(now, userId);
        if (!transitionResult.IsSuccess)
        {
            logger.LogError("Не удалось завершить приемку приходного ордера: {ErrorMessage}", transitionResult.Error?.Message);
            return transitionResult;
        }

        var movementsResult = CreateReceivingMovements(order, now);
        if (!movementsResult.IsSuccess)
        {
            return movementsResult.Error!;
        }

        var movements = movementsResult.Value!;
        dbContext.InventoryMovements.AddRange(movements);

        var balanceAndTurnoverResult = await inventoryPostingService
            .PostInventoryMovementsAsync(movements, dbContext, ct);

        if (!balanceAndTurnoverResult.IsSuccess)
            return balanceAndTurnoverResult;

        if (order.HasPlanFactDifference)
        {
            var externalItemsUpdateResult = await executionSink.UpdateItemsAsync(
                order.Id,
                order.Items,
                ct);

            if (!externalItemsUpdateResult.IsSuccess)
            {
                logger.LogError("Не удалось обновить строки приходного ордера в 1С: {ErrorMessage}", externalItemsUpdateResult.Error?.Message);
                return externalItemsUpdateResult;
            }
        }

        var externalResult = await executionSink.SetReceivedAsync(order.Id, ct);

        if (!externalResult.IsSuccess)
        {
            logger.LogError("Не удалось завершить приемку документа в 1С: {ErrorMessage}", externalResult.Error?.Message);
            return externalResult;
        }

        return OperationResult.Success();
    }

    private static OperationResult EnsureSynchronizationAllowsWork(ReceivingOrder order) =>
        order.SynchronizationLevel switch
        {
            OrderSynchronizationLevel.Synchronized => OperationResult.Success(),
            OrderSynchronizationLevel.RequiresOperatorDecision => OperationError.Conflict(
                "Приходный ордер требует решения оператора по изменениям 1С."),
            _ => OperationError.Conflict(
                "Работа с приходным ордером заблокирована из-за расхождений с 1С.")
        };

    public async Task<OperationResult> UpdateOrderItemFactQuantityAsync(
        Guid receivingOrderId,
        int lineNumber,
        decimal factQuantity,
        string? comment,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var result = await StageUpdateItemFactQuantityAsync(
            dbContext,
            receivingOrderId,
            lineNumber,
            factQuantity,
            comment,
            ct);
        if (!result.IsSuccess)
        {
            return result;
        }

        return await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
    }

    internal async Task<OperationResult> StageUpdateItemFactQuantityAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        int lineNumber,
        decimal factQuantity,
        string? comment,
        CancellationToken ct)
    {
        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        return order.UpdateItemFact(lineNumber, factQuantity, comment);
    }

    internal async Task<OperationResult> StageIncrementItemFactAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        int lineNumber,
        CancellationToken ct)
    {
        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        return order.IncrementItemFact(lineNumber);
    }

    internal async Task<OperationResult> StageSetItemFactQuantityAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        int lineNumber,
        decimal factQuantity,
        CancellationToken ct)
    {
        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        return order.UpdateItemFactQuantity(lineNumber, factQuantity);
    }

    public async Task<OperationResult> UpdateOrderItemCommentAsync(
        Guid receivingOrderId,
        int lineNumber,
        string? comment,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var result = await StageUpdateItemCommentAsync(
            dbContext,
            receivingOrderId,
            lineNumber,
            comment,
            ct);
        if (!result.IsSuccess)
        {
            return result;
        }

        return await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
    }

    internal async Task<OperationResult> StageUpdateItemCommentAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        int lineNumber,
        string? comment,
        CancellationToken ct)
    {
        var order = await LoadOrderAsync(dbContext, orderId, ct);
        if (order is null)
        {
            return OperationError.NotFound($"Приходный ордер '{orderId}' не найден.");
        }

        return order.UpdateItemComment(lineNumber, comment);
    }

    private static async Task<OperationResult> StageSetReceivingLocationAsync(
        ApplicationDbContext dbContext,
        ReceivingOrder order,
        Guid receivingLocationId,
        CancellationToken ct)
    {
        var validationResult = await ReceivingOrderLocationPolicy.RequireReceivingLocationAsync(
            dbContext,
            order,
            receivingLocationId,
            ct);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        return order.SetReceivingLocation(receivingLocationId);
    }

    private static Task<ReceivingOrder?> LoadOrderAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        CancellationToken ct) =>
        dbContext.ReceivingOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

    private static OperationResult<List<InventoryMovement>> CreateReceivingMovements(
        ReceivingOrder order,
        DateTimeOffset createdAtUtc)
    {
        var movements = new List<InventoryMovement>();
        foreach (var item in order.Items.Where(x => x.FactQuantity > 0))
        {
            var movementResult = InventoryMovement.Create(
                Guid.NewGuid(),
                order.WarehouseId,
                null,
                order.ReceivingLocationId,
                item.StockKeepingUnitId,
                item.FactQuantity!.Value,
                createdAtUtc,
                RecorderType.ReceivingOrder,
                order.Id,
                item.LineNumber);
            if (!movementResult.IsSuccess)
            {
                return movementResult.Error!;
            }

            movements.Add(movementResult.Value!);
        }

        return movements;
    }
}
