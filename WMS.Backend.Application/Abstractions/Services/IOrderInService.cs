using WMS.Backend.Application.Services.OrderInServices;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInService
    {
        Task<OrderIn> CreateOrderAsync(OrderIn order);

        Task UpdateOrderAsync(Guid id, OrderIn order);

        Task DeleteOrderAsync(Guid id);

        Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery);

        Task<OrderIn?> GetOrderByIdAsync(Guid id);
    }
}
