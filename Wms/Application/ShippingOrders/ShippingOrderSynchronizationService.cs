using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.Application.Persistence;
using Wms.Common;
using Wms.Data;
using Wms.Domain;
using Wms.Domain.Enums;

namespace Wms.Application.ShippingOrders;

public sealed class ShippingOrderSynchronizationService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IShippingOrderSource orderSource,
    ILogger<ShippingOrderSynchronizationService> logger)
{
    public async Task<OperationResult<OrderSynchronizationAssessment>> CheckAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        OperationResult<ShippingOrderImportSnapshot> snapshotResult =
            await orderSource.GetSnapshotAsync(orderId, ct);
        return snapshotResult.IsSuccess
            ? await ApplySnapshotAsync(snapshotResult.Value!, allowCreate: false, ct)
            : snapshotResult.Error!;
    }

    public async Task<OperationResult<OrderSynchronizationAssessment>> ImportNotificationAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        OperationResult<ShippingOrderImportSnapshot> snapshotResult =
            await orderSource.GetSnapshotAsync(orderId, ct);
        return snapshotResult.IsSuccess
            ? await ApplySnapshotAsync(snapshotResult.Value!, allowCreate: true, ct)
            : snapshotResult.Error!;
    }

    public async Task<OperationResult> AcknowledgeAsync(
        Guid orderId,
        string expectedFingerprint,
        string userId,
        CancellationToken ct = default)
    {
        OperationResult<ShippingOrderImportSnapshot> snapshotResult =
            await orderSource.GetSnapshotAsync(orderId, ct);
        if (!snapshotResult.IsSuccess)
            return snapshotResult.Error!;

        ShippingOrderImportSnapshot snapshot = snapshotResult.Value!;
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        ShippingOrder? order = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .Include(x => x.BaseItems)
            .FirstOrDefaultAsync(x => x.Id == snapshot.Id, ct);
        if (order is null)
            return OperationError.NotFound($"Расходный ордер '{snapshot.Id}' не найден в WMS.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        OrderSynchronizationAssessment assessment = order.AssessSynchronization(snapshot, now);
        if (!string.Equals(assessment.Fingerprint, expectedFingerprint, StringComparison.Ordinal))
        {
            OperationResult<ShippingOrderReconciliation> reconciliationResult = order.Reconcile(snapshot, now);
            if (!reconciliationResult.IsSuccess)
                return reconciliationResult.Error!;

            OperationResult saveResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
            return saveResult.IsSuccess
                ? OperationError.Conflict("Расходный ордер в 1С изменился. Просмотрите новые расхождения.")
                : saveResult;
        }

        OperationResult acknowledgeResult = order.AcknowledgeSynchronization(
            snapshot,
            assessment,
            DateTimeOffset.UtcNow,
            userId);
        return acknowledgeResult.IsSuccess
            ? await ApplicationPersistence.SaveChangesAsync(dbContext, ct)
            : acknowledgeResult;
    }

    internal Task<OperationResult> PersistReadyForShipmentCheckpointAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        CancellationToken ct) =>
        PersistCompletionCheckpointAsync(
            dbContext,
            order,
            ShippingSynchronizationTarget.ReadyForShipment,
            ct);

    internal Task<OperationResult> PersistShippedCheckpointAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        CancellationToken ct) =>
        PersistCompletionCheckpointAsync(
            dbContext,
            order,
            ShippingSynchronizationTarget.Shipped,
            ct);

    private async Task<OperationResult> PersistCompletionCheckpointAsync(
        ApplicationDbContext dbContext,
        ShippingOrder order,
        ShippingSynchronizationTarget target,
        CancellationToken ct)
    {
        OperationResult<ShippingOrderImportSnapshot> snapshotResult =
            await orderSource.GetSnapshotAsync(order.Id, ct);
        if (!snapshotResult.IsSuccess)
            return snapshotResult.Error!;

        ShippingOrderImportSnapshot snapshot = snapshotResult.Value!;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OrderSynchronizationAssessment sourceAssessment = order.AssessSynchronization(snapshot, now);
        OrderSynchronizationAssessment targetAssessment = target switch
        {
            ShippingSynchronizationTarget.ReadyForShipment =>
                ShippingOrderSynchronizationComparer.CompareReadyForShipmentTarget(order, snapshot),
            _ => ShippingOrderSynchronizationComparer.CompareShippedTarget(order, snapshot)
        };
        OrderSynchronizationAssessment assessment = sourceAssessment.Level == OrderSynchronizationLevel.Synchronized
            ? sourceAssessment
            : targetAssessment.Level == OrderSynchronizationLevel.Synchronized
                ? targetAssessment
                : sourceAssessment;

        order.ApplySynchronizationAssessment(assessment, now);
        OperationResult saveResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
        return saveResult.IsSuccess ? EnsureAllowsWork(order) : saveResult;
    }

    private async Task<OperationResult<OrderSynchronizationAssessment>> ApplySnapshotAsync(
        ShippingOrderImportSnapshot snapshot,
        bool allowCreate,
        CancellationToken ct)
    {
        using IDisposable? scope = logger.BeginScope("ShippingOrder Synchronize {OrderId}", snapshot.Id);
        using Activity? activity = AppTracing.StartActivity(
            "ShippingOrder.Synchronize",
            nameof(ShippingOrderSynchronizationService));

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        ShippingOrder? existingOrder = await dbContext.ShippingOrders
            .Include(x => x.Items)
            .Include(x => x.BaseItems)
            .FirstOrDefaultAsync(x => x.Id == snapshot.Id, ct);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (existingOrder is null)
        {
            if (!allowCreate)
                return OperationError.NotFound($"Расходный ордер '{snapshot.Id}' не найден в WMS.");

            OperationResult<ShippingOrder> creationResult = ShippingOrder.Create(snapshot, now);
            if (!creationResult.IsSuccess)
                return creationResult.Error!;

            ShippingOrder createdOrder = creationResult.Value!;
            dbContext.ShippingOrders.Add(createdOrder);
            OperationResult saveCreationResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
            return saveCreationResult.IsSuccess
                ? ShippingOrderSynchronizationComparer.Compare(createdOrder, snapshot)
                : saveCreationResult.Error!;
        }

        OperationResult<ShippingOrderReconciliation> reconciliationResult = existingOrder.Reconcile(snapshot, now);
        if (!reconciliationResult.IsSuccess)
            return reconciliationResult.Error!;

        OrderSynchronizationAssessment assessment = existingOrder.AssessSynchronization(snapshot, now);
        if (reconciliationResult.Value == ShippingOrderReconciliation.Unchanged)
        {
            logger.LogDebug("Изменения документа в 1С не обнаружены");
            return assessment;
        }

        OperationResult saveResult = await ApplicationPersistence.SaveChangesAsync(dbContext, ct);
        if (!saveResult.IsSuccess)
            return saveResult.Error!;

        if (reconciliationResult.Value == ShippingOrderReconciliation.DifferencesDetected)
        {
            logger.LogWarning(
                "При сверке расходного ордера с 1С обнаружены расхождения. Уровень: {Level}, поля: {Fields}",
                assessment.Level,
                assessment.Differences.Select(x => x.FieldCode).ToArray());
        }

        return assessment;
    }

    private static OperationResult EnsureAllowsWork(ShippingOrder order) =>
        order.SynchronizationLevel switch
        {
            OrderSynchronizationLevel.Synchronized => OperationResult.Success(),
            OrderSynchronizationLevel.RequiresOperatorDecision => OperationError.Conflict(
                "Расходный ордер требует решения оператора по изменениям 1С."),
            _ => OperationError.Conflict(
                "Работа с расходным ордером заблокирована из-за расхождений с 1С.")
        };

    private enum ShippingSynchronizationTarget
    {
        ReadyForShipment,
        Shipped
    }
}
