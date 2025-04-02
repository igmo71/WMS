using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderService
    {
        Task<Order> CreateAsync(CreateOrderCommand createCommand);
        Task<bool> UpdateAsync(Guid id, Order order);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Order>> GetListAsync(OrderQuery orderQuery);
        Task<Order?> GetByIdAsync(Guid id);
    }
}
