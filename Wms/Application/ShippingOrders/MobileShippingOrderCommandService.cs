using System.Globalization;
using Wms.Application.Commands;
using Wms.Common;
using Wms.Domain;

namespace Wms.Application.ShippingOrders;

public sealed class MobileShippingOrderCommandService(
    CommandExecutor commandExecutor,
    PickingCommandService pickingCommandService)
{
    private const string AddPickingMovementCommand = "shipping-order.add-picking-movement";
    private const string DeletePickingMovementCommand = "shipping-order.delete-picking-movement";

    public Task<OperationResult<Guid>> AddPickingMovementAsync(
        Guid orderId,
        int lineNumber,
        string? sourceStorageLocationBarcode,
        decimal quantity,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default)
    {
        if (!StorageLocation.TryParseBarcode(sourceStorageLocationBarcode, out var sourceStorageLocationId))
        {
            return Task.FromResult<OperationResult<Guid>>(
                OperationError.Invalid("Некорректный QR-код ячейки."));
        }

        return commandExecutor.ExecuteAsync(
            AddPickingMovementCommand,
            clientRequestId,
            Hash(orderId, lineNumber, sourceStorageLocationId, quantity),
            userId,
            async (dbContext, token) =>
            {
                var result = await pickingCommandService.StageAddPickingMovementAsync(
                    dbContext,
                    orderId,
                    lineNumber,
                    sourceStorageLocationId,
                    quantity,
                    token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            },
            ct);
    }

    public Task<OperationResult<Guid>> DeletePickingMovementAsync(
        Guid orderId,
        Guid movementId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        commandExecutor.ExecuteAsync(
            DeletePickingMovementCommand,
            clientRequestId,
            Hash(orderId, movementId),
            userId,
            async (dbContext, token) =>
            {
                var result = await pickingCommandService.StageDeletePickingMovementAsync(
                    dbContext,
                    orderId,
                    movementId,
                    token);
                return result.IsSuccess ? movementId : result.Error!;
            },
            ct);

    private static string Hash(Guid orderId, Guid movementId) =>
        CommandExecutor.ComputeHash(string.Join(
            '|',
            orderId.ToString("N"),
            movementId.ToString("N")));

    private static string Hash(
        Guid orderId,
        int lineNumber,
        Guid sourceStorageLocationId,
        decimal quantity) =>
        CommandExecutor.ComputeHash(string.Join(
            '|',
            orderId.ToString("N"),
            lineNumber.ToString(CultureInfo.InvariantCulture),
            sourceStorageLocationId.ToString("N"),
            quantity.ToString("G29", CultureInfo.InvariantCulture)));
}
