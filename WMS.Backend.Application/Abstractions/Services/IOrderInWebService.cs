using WMS.Backend.Application.Services.OrderInServices;
using Dto = WMS.Shared.Models.Documents;


namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IOrderInWebService
    {
        Task<Dto.OrderIn> CreateOrderAsync(Dto.OrderIn order);
        Task UpdateOrderAsync(Guid id, Dto.OrderIn order);
        Task DeleteOrderAsync(Guid id);
        Task<List<Dto.OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery);
        Task<Dto.OrderIn?> GetOrderByIdAsync(Guid id);
    }
}
