using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInService
    {
        Task<OrderIn> CreateOrderAsync(CreateOrderInCommand createCommand);
        Task UpdateOrderAsync(Guid id, OrderIn order);
        Task DeleteOrderAsync(Guid id);
        Task<List<OrderIn>> GetOrderListAsync(OrderQuery orderQuery);
        Task<OrderIn?> GetOrderByIdAsync(Guid id);
    }
}
