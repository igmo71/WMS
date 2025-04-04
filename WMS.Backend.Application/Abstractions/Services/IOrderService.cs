using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CreateOrderCommand createCommand);
        Task<bool> UpdateOrderAsync(Guid id, Order order);
        Task<bool> DeleteOrderAsync(Guid id);
        Task<List<Order>> GetOrderListAsync(OrderQuery orderQuery);
        Task<Order?> GetOrderByIdAsync(Guid id);
    }
}
