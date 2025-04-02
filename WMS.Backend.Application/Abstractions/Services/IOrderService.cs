using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderService
    {
        Task<Order> CreateAsync(Order newOrder);
        Task<bool> UpdateAsync(Guid id, Order order);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Order>> GetListAsync(OrderQuery orderQuery);
        Task<Order?> GetByIdAsync(Guid id);
    }
}
