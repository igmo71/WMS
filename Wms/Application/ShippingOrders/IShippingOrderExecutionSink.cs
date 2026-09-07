using Wms.Common;
using Wms.Domain;

namespace Wms.Application.ShippingOrders;

public interface IShippingOrderExecutionSink
{
    Task<OperationResult> SetReadyForPickingAsync(Guid orderId, CancellationToken ct);

    Task<OperationResult> UpdateItemsAsync(ShippingOrder order, CancellationToken ct);

    Task<OperationResult> SetReadyForShipmentAsync(Guid orderId, CancellationToken ct);

    Task<OperationResult> SetShippedAsync(Guid orderId, CancellationToken ct);
}
