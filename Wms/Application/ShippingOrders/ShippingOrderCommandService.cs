using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.Application.Inventory.Movements;
using Wms.Application.Persistence;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.ShippingOrders;

public class ShippingOrderCommandService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    InventoryPostingService inventoryPostingService,
    ShippingOrderSynchronizationService synchronizationService,
    IShippingOrderExecutionSink executionSink,
    ILogger<ShippingOrderCommandService> logger)
{
    public async Task<OperationResult> StartPickingAsync(
        Guid orderId,
        Guid shippingLocationId,
        string userId,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        OperationResult result = await StageStartPickingAsync(
            dbContext,
            orderId,
            shippingLocationId,
            userId,
            ct);

        return result.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : result;
    }

    internal async Task<OperationResult> StageStartPickingAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        Guid shippingLocationId,
        string userId,
        CancellationToken ct)
    {
        using IDisposable? scope = logger.BeginScope("ShippingOrder StartPicking {OrderId}", orderId);
        using Activity? activity = AppTracing.StartActivity("ShippingOrder.StartPicking", nameof(ShippingOrderCommandService));

        ShippingOrder? order = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null)
        {
            logger.LogError("Расходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Расходный ордер '{orderId}' не найден.");
        }

        OperationResult synchronizationResult = EnsureSynchronizationAllowsWork(order);
        if (!synchronizationResult.IsSuccess)
            return synchronizationResult;

        OperationResult locationResult = await StageSetShippingLocationAsync(
            dbContext,
            order,
            shippingLocationId,
            ct);
        if (!locationResult.IsSuccess)
        {
            return locationResult;
        }

        return await StageSetReadyForPickingAsync(order, userId, ct);
    }

    private async Task<OperationResult> StageSetReadyForPickingAsync(
        ShippingOrder order,
        string userId,
        CancellationToken ct)
    {
        OperationResult transitionResult = order.SetReadyForPicking(DateTimeOffset.UtcNow, userId);
        if (!transitionResult.IsSuccess)
        {
            logger.LogError("Не удалось подготовить расходный ордер к отбору: {ErrorMessage}", transitionResult.Error?.Message);
            return transitionResult;
        }

        OperationResult externalResult = await executionSink.SetReadyForPickingAsync(order.Id, ct);

        if (!externalResult.IsSuccess)
        {
            logger.LogError("Не удалось подготовить документ 1С к отбору: {ErrorMessage}", externalResult.Error?.Message);
            return externalResult;
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> SetReadyForShipmentAsync(Guid orderId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        OperationResult result = await ExecuteReadyForShipmentWithSynchronizationCheckpointAsync(
            dbContext,
            orderId,
            userId,
            ct);

        return result.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : result;
    }

    internal async Task<OperationResult> ExecuteReadyForShipmentWithSynchronizationCheckpointAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        string userId,
        CancellationToken ct)
    {
        using IDisposable? scope = logger.BeginScope("ShippingOrder SetReadyForShipment {OrderId}", orderId);
        using Activity? activity = AppTracing.StartActivity("ShippingOrder.SetReadyForShipment", nameof(ShippingOrderCommandService));

        ShippingOrder? existingOrder = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .Include(x => x.BaseItems)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (existingOrder is null)
        {
            logger.LogError("Расходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Расходный ордер '{orderId}' не найден.");
        }

        OperationResult synchronizationResult = await synchronizationService.PersistReadyForShipmentCheckpointAsync(
            dbContext,
            existingOrder,
            ct);
        if (!synchronizationResult.IsSuccess)
            return synchronizationResult;

        return await StageSetReadyForShipmentAsync(dbContext, existingOrder, userId, ct);
    }

    private async Task<OperationResult> StageSetReadyForShipmentAsync(
        ApplicationDbContext dbContext,
        ShippingOrder existingOrder,
        string userId,
        CancellationToken ct)
    {
        List<InventoryMovement> draftPickingMovements = await dbContext.InventoryMovements
            .Where(x => x.PostedAtUtc == null
                && x.RecorderType == RecorderType.ShippingOrder
                && x.RecorderId == existingOrder.Id)
            .ToListAsync(ct);

        OperationResult shippingLocationResult = await ShippingOrderLocationPolicy.RequireShippingLocationAsync(
            dbContext,
            existingOrder,
            existingOrder.ShippingLocationId,
            ct);
        if (!shippingLocationResult.IsSuccess)
        {
            return shippingLocationResult;
        }

        OperationResult routesResult = await ShippingOrderLocationPolicy.ValidatePickingRoutesAsync(
            dbContext,
            existingOrder,
            draftPickingMovements,
            ct);
        if (!routesResult.IsSuccess)
        {
            return routesResult;
        }

        OperationResult transitionResult = existingOrder.SetReadyForShipment(
            draftPickingMovements,
            DateTimeOffset.UtcNow,
            userId);
        if (!transitionResult.IsSuccess)
        {
            logger.LogError("Не удалось подготовить расходный ордер к отгрузке: {ErrorMessage}", transitionResult.Error?.Message);
            return transitionResult;
        }

        OperationResult balanceAndTurnoverResult = await inventoryPostingService
            .PostInventoryMovementsAsync(draftPickingMovements, dbContext, ct);

        if (!balanceAndTurnoverResult.IsSuccess)
        {
            return balanceAndTurnoverResult;
        }

        OperationResult externalItemsUpdateResult = await executionSink.UpdateItemsAsync(existingOrder, ct);

        if (!externalItemsUpdateResult.IsSuccess)
        {
            logger.LogError("Не удалось обновить строки расходного ордера в 1С: {ErrorMessage}", externalItemsUpdateResult.Error?.Message);
            return externalItemsUpdateResult;
        }

        OperationResult externalResult = await executionSink.SetReadyForShipmentAsync(existingOrder.Id, ct);

        if (!externalResult.IsSuccess)
        {
            logger.LogError("Не удалось подготовить документ 1С к отгрузке: {ErrorMessage}", externalResult.Error?.Message);
            return externalResult;
        }

        // 1C is updated before the local save by the established integration boundary.
        // There is no outbox or distributed transaction; target-state calls are repeat-safe
        // so the same command can recover after external success and local save failure.
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetShippedAsync(Guid orderId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        OperationResult result = await ExecuteShipmentWithSynchronizationCheckpointAsync(
            dbContext,
            orderId,
            userId,
            ct: ct);

        return result.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : result;
    }

    internal async Task<OperationResult> ExecuteShipmentWithSynchronizationCheckpointAsync(
        ApplicationDbContext dbContext,
        Guid orderId,
        string userId,
        CancellationToken ct)
    {
        using IDisposable? scope = logger.BeginScope("ShippingOrder SetShipped {OrderId}", orderId);
        using Activity? activity = AppTracing.StartActivity("ShippingOrder.SetShipped", nameof(ShippingOrderCommandService));

        ShippingOrder? existingOrder = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .Include(x => x.BaseItems)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (existingOrder is null)
        {
            logger.LogError("Расходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Расходный ордер '{orderId}' не найден.");
        }

        OperationResult synchronizationResult = await synchronizationService.PersistShippedCheckpointAsync(
            dbContext,
            existingOrder,
            ct);
        if (!synchronizationResult.IsSuccess)
            return synchronizationResult;

        return await StageSetShippedAsync(dbContext, existingOrder, userId, ct);
    }

    private async Task<OperationResult> StageSetShippedAsync(
        ApplicationDbContext dbContext,
        ShippingOrder existingOrder,
        string userId,
        CancellationToken ct)
    {
        OperationResult locationResult = await ShippingOrderLocationPolicy.RequireShippingLocationAsync(
            dbContext,
            existingOrder,
            existingOrder.ShippingLocationId,
            ct);
        if (!locationResult.IsSuccess)
        {
            return locationResult;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationResult transitionResult = existingOrder.SetShipped(now, userId);
        if (!transitionResult.IsSuccess)
        {
            logger.LogError("Не удалось завершить отгрузку расходного ордера: {ErrorMessage}", transitionResult.Error?.Message);
            return transitionResult;
        }

        OperationResult<List<InventoryMovement>> movementsResult = CreateShippingMovements(existingOrder, now);
        if (!movementsResult.IsSuccess)
        {
            return movementsResult.Error!;
        }

        List<InventoryMovement> movements = movementsResult.Value!;
        dbContext.InventoryMovements.AddRange(movements);

        OperationResult balanceAndTurnoverResult = await inventoryPostingService
            .PostInventoryMovementsAsync(movements, dbContext, ct);

        if (!balanceAndTurnoverResult.IsSuccess)
        {
            return balanceAndTurnoverResult;
        }

        OperationResult externalResult = await executionSink.SetShippedAsync(existingOrder.Id, ct);

        if (!externalResult.IsSuccess)
        {
            logger.LogError("Не удалось завершить отгрузку документа в 1С: {ErrorMessage}", externalResult.Error?.Message);
            return externalResult;
        }

        // 1C is updated before the local save by the established integration boundary.
        // There is no outbox or distributed transaction; target-state calls are repeat-safe
        // so the same command can recover after external success and local save failure.
        return OperationResult.Success();
    }

    private static OperationResult EnsureSynchronizationAllowsWork(ShippingOrder order) =>
        order.SynchronizationLevel switch
        {
            OrderSynchronizationLevel.Synchronized => OperationResult.Success(),
            OrderSynchronizationLevel.RequiresOperatorDecision => OperationError.Conflict(
                "Расходный ордер требует решения оператора по изменениям 1С."),
            _ => OperationError.Conflict(
                "Работа с расходным ордером заблокирована из-за расхождений с 1С.")
        };

    private static async Task<OperationResult> StageSetShippingLocationAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        Guid shippingLocationId,
        CancellationToken ct)
    {
        var locationResult = await ShippingOrderLocationPolicy.RequireShippingLocationAsync(
            dbContext,
            order,
            shippingLocationId,
            ct);
        if (!locationResult.IsSuccess)
        {
            return locationResult;
        }

        return order.SetShippingLocation(shippingLocationId);
    }

    public async Task<OperationResult> RollbackAsync(
        Guid orderId,
        string reason,
        string userId,
        CancellationToken ct = default)
    {
        using IDisposable? scope = logger.BeginScope("ShippingOrder Rollback {OrderId}", orderId);
        using Activity? activity = AppTracing.StartActivity("ShippingOrder.Rollback", nameof(ShippingOrderCommandService));

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        ShippingOrder? order = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct);

        if (order is null)
        {
            logger.LogError("Расходный ордер {OrderId} не найден", orderId);
            return OperationError.NotFound($"Расходный ордер '{orderId}' не найден.");
        }

        List<InventoryMovement> draftMovements = await dbContext.InventoryMovements
            .Where(x => x.PostedAtUtc == null
                && x.RecorderType == RecorderType.ShippingOrder
                && x.RecorderId == order.Id)
            .ToListAsync(ct);

        List<InventoryMovement> postedMovements = await dbContext.InventoryMovements
            .Where(x => x.PostedAtUtc != null
                && x.RecorderType == RecorderType.ShippingOrder
                && x.RecorderId == order.Id)
            .ToListAsync(ct);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationResult<List<InventoryMovement>> rollbackResult = order.Rollback(
            reason,
            userId,
            now,
            draftMovements,
            postedMovements);
        if (!rollbackResult.IsSuccess)
        {
            logger.LogError("Не удалось отменить операцию расходного ордера: {ErrorMessage}", rollbackResult.Error?.Message);
            return rollbackResult.Error!;
        }

        List<InventoryMovement> compensationMovements = rollbackResult.Value!;
        OperationResult routesResult = await ShippingOrderLocationPolicy.ValidateRollbackRoutesAsync(
            dbContext,
            order,
            compensationMovements,
            ct);
        if (!routesResult.IsSuccess)
        {
            return routesResult;
        }

        dbContext.InventoryMovements.RemoveRange(draftMovements);

        if (compensationMovements.Count > 0)
        {
            dbContext.InventoryMovements.AddRange(compensationMovements);

            OperationResult postingResult = await inventoryPostingService
                .PostInventoryMovementsAsync(compensationMovements, dbContext, ct);

            if (!postingResult.IsSuccess)
            {
                logger.LogError("При отмене операции расходного ордера не удалось компенсировать движения: {ErrorMessage}", postingResult.Error?.Message);
                return postingResult;
            }
        }

        var saveResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
        if (!saveResult.IsSuccess)
        {
            return saveResult;
        }

        logger.LogInformation("Операция расходного ордера отменена пользователем {UserId}. Причина: {Reason}", userId, reason.Trim());
        return OperationResult.Success();
    }

    private static OperationResult<List<InventoryMovement>> CreateShippingMovements(
        ShippingOrder order,
        DateTimeOffset createdAtUtc)
    {
        var movements = new List<InventoryMovement>();
        foreach (ShippingOrderItem? item in order.Items.Where(x => x.FactQuantity != 0))
        {
            OperationResult<InventoryMovement> movementResult = InventoryMovement.Create(
                Guid.NewGuid(),
                order.WarehouseId,
                order.ShippingLocationId,
                null,
                item.StockKeepingUnitId,
                item.FactQuantity,
                createdAtUtc,
                RecorderType.ShippingOrder,
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
