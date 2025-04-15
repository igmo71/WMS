using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.MessageBus.Abstractions
{
    public interface IOrderInQueryService
    {
        Task OrderInGetListQueryProduce(OrderInGetListQuery orderQuery);
        Task OrderInGetListResponseProduce(List<OrderIn>? orders, byte[]? correlationId = null);
    }
}
