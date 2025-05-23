using WMS.Backend.Application.Services.OrderInServices;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInService
    {
        Task<Dto.OrderIn> CreateOrderInAsync(Dto.OrderIn order);
        Task UpdateOrderInAsync(Guid id, Dto.OrderIn order);
        Task DeleteOrderInAsync(Guid id);
        Task<Dto.OrderIn?> GetOrderAsync(Guid id);
        Task<List<Dto.OrderIn>> GetListOrderInAsync(OrderInGetListQuery orderQuery);
    }
}
