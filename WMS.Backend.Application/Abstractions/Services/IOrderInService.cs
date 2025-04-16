using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInService
    {
        Task<OrderIn> CreateOrderAsync(OrderInCreateCommand createCommand, byte[]? correlationId = null);
        Task UpdateOrderAsync(Guid id, OrderIn order);
        Task DeleteOrderAsync(Guid id, byte[]? correlationId = null);
        Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery);
        Task<OrderIn?> GetOrderByIdAsync(Guid id);
    }
}
