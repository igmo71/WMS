using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderInProductRepository
    {
        Task<int> CreateRangeAsync(Guid orderId, List<CreateOrderInProduct>? createOrderInProducts);
        Task<List<OrderInProduct>> GetListAsync(Guid orderId);
    }
}
