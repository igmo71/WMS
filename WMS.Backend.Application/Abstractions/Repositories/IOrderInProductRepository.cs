using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderInProductRepository
    {
        Task<int> CreateRangeAsync(Guid orderId, List<OrderInProductCreateCommand>? orderInProductCreateCommand);
        Task<int> UpdateRangeAsync(Guid orderId, List<OrderInProduct>? products);
        Task<int> DeleteRangeAsync(Guid orderId);

        Task<List<OrderInProduct>> GetListAsync(Guid orderId);
    }
}
