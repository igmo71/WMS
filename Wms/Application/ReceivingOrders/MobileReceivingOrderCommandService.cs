using System.Globalization;
using Wms.Application.Commands;
using Wms.Common;

namespace Wms.Application.ReceivingOrders;

public sealed class MobileReceivingOrderCommandService(
    CommandExecutor commandExecutor,
    ReceivingOrderCommandService receivingOrderCommandService,
    PutawayCommandService putawayCommandService)
{
    private const string IncrementFactCommand = "receiving-order.increment-fact";
    private const string SetFactCommand = "receiving-order.set-fact";
    private const string StartPutawayCommand = "receiving-order.start-putaway";
    private const string AddPutawayMovementCommand = "receiving-order.add-putaway-movement";
    private const string DeletePutawayMovementCommand = "receiving-order.delete-putaway-movement";
    private const string CompletePutawayCommand = "receiving-order.complete-putaway";

    public Task<OperationResult<Guid>> IncrementItemFactAsync(
        Guid orderId,
        int lineNumber,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteOrderActionAsync(
            IncrementFactCommand,
            orderId,
            clientRequestId,
            userId,
            Hash(orderId, lineNumber),
            (dbContext, token) => receivingOrderCommandService.StageIncrementItemFactAsync(
                dbContext,
                orderId,
                lineNumber,
                token),
            ct);

    public Task<OperationResult<Guid>> SetItemFactQuantityAsync(
        Guid orderId,
        int lineNumber,
        decimal factQuantity,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteOrderActionAsync(
            SetFactCommand,
            orderId,
            clientRequestId,
            userId,
            Hash(orderId, lineNumber, factQuantity),
            (dbContext, token) => receivingOrderCommandService.StageSetItemFactQuantityAsync(
                dbContext,
                orderId,
                lineNumber,
                factQuantity,
                token),
            ct);

    public Task<OperationResult<Guid>> StartPutawayAsync(
        Guid orderId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteOrderActionAsync(
            StartPutawayCommand,
            orderId,
            clientRequestId,
            userId,
            Hash(orderId),
            (dbContext, token) => putawayCommandService.StageStartAsync(
                dbContext,
                orderId,
                userId,
                token),
            ct);

    public Task<OperationResult<Guid>> AddPutawayMovementAsync(
        Guid orderId,
        int lineNumber,
        Guid destinationStorageLocationId,
        decimal quantity,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        commandExecutor.ExecuteAsync(
            AddPutawayMovementCommand,
            clientRequestId,
            Hash(orderId, lineNumber, destinationStorageLocationId, quantity),
            userId,
            async (dbContext, token) =>
            {
                var result = await putawayCommandService.StageAddMovementAsync(
                    dbContext,
                    orderId,
                    lineNumber,
                    destinationStorageLocationId,
                    quantity,
                    token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> DeletePutawayMovementAsync(
        Guid orderId,
        Guid movementId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        commandExecutor.ExecuteAsync(
            DeletePutawayMovementCommand,
            clientRequestId,
            Hash(orderId, movementId),
            userId,
            async (dbContext, token) =>
            {
                var result = await putawayCommandService.StageDeleteMovementAsync(
                    dbContext,
                    orderId,
                    movementId,
                    token);
                return result.IsSuccess ? movementId : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> CompletePutawayAsync(
        Guid orderId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteOrderActionAsync(
            CompletePutawayCommand,
            orderId,
            clientRequestId,
            userId,
            Hash(orderId),
            (dbContext, token) => putawayCommandService.StageCompleteAsync(
                dbContext,
                orderId,
                userId,
                token),
            ct);

    private Task<OperationResult<Guid>> ExecuteOrderActionAsync(
        string commandType,
        Guid orderId,
        Guid clientRequestId,
        string userId,
        string requestHash,
        Func<Data.ApplicationDbContext, CancellationToken, Task<OperationResult>> stageAction,
        CancellationToken ct) =>
        commandExecutor.ExecuteAsync(
            commandType,
            clientRequestId,
            requestHash,
            userId,
            async (dbContext, token) =>
            {
                var result = await stageAction(dbContext, token);
                return result.IsSuccess ? orderId : result.Error!;
            },
            ct);

    private static string Hash(params Guid[] ids) =>
        CommandExecutor.ComputeHash(string.Join('|', ids.Select(x => x.ToString("N"))));

    private static string Hash(Guid orderId, int lineNumber) =>
        CommandExecutor.ComputeHash(string.Join(
            '|',
            orderId.ToString("N"),
            lineNumber.ToString(CultureInfo.InvariantCulture)));

    private static string Hash(Guid orderId, int lineNumber, decimal quantity) =>
        CommandExecutor.ComputeHash(string.Join(
            '|',
            orderId.ToString("N"),
            lineNumber.ToString(CultureInfo.InvariantCulture),
            quantity.ToString("G29", CultureInfo.InvariantCulture)));

    private static string Hash(
        Guid orderId,
        int lineNumber,
        Guid destinationStorageLocationId,
        decimal quantity) =>
        CommandExecutor.ComputeHash(string.Join(
            '|',
            orderId.ToString("N"),
            lineNumber.ToString(CultureInfo.InvariantCulture),
            destinationStorageLocationId.ToString("N"),
            quantity.ToString("G29", CultureInfo.InvariantCulture)));
}
