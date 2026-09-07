using Wms.Common;
using Wms.Domain;

namespace Wms.Application.ReceivingOrders;

public interface IReceivingOrderExecutionSink
{
    Task<OperationResult> SetInReceivingAsync(Guid orderId, CancellationToken ct);

    Task<OperationResult> UpdateItemsAsync(
        Guid orderId,
        IReadOnlyCollection<ReceivingOrderItem> items,
        CancellationToken ct);

    Task<OperationResult> SetReceivedAsync(Guid orderId, CancellationToken ct);
}
