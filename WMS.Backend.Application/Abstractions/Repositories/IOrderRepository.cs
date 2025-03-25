using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<int> UpdateAsync(Guid id, Order order);
        Task<int> DeleteAsync(Guid id);
        Task<List<Order>> GetListAsync(OrderQuery? orderQuery);
        Task<Order?> GetByIdAsync(Guid id);
    }
}
