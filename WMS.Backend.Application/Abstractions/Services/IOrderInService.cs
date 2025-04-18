using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInService
    {
        Task<Dto.OrderIn> CreateOrderByDtoAsync(Dto.OrderIn order);
        Task<OrderIn> CreateOrderAsync(OrderIn order);

        Task UpdateOrderByDtoAsync(Guid id, Dto.OrderIn order);
        Task UpdateOrderAsync(Guid id, OrderIn order);

        Task DeleteOrderAsync(Guid id);

        Task<List<Dto.OrderIn>> GetOrderDtoListAsync(OrderInGetListQuery orderQuery);
        Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery);

        Task<Dto.OrderIn?> GetOrderDtoByIdAsync(Guid id);
        Task<OrderIn?> GetOrderByIdAsync(Guid id);
    }
}
