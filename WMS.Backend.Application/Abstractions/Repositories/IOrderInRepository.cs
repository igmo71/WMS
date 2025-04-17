using Microsoft.EntityFrameworkCore.Storage;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderInRepository
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<OrderIn> CreateAsync(OrderIn order);
        Task UpdateAsync(Guid id, OrderIn order);
        Task DeleteAsync(Guid id);
        Task<List<OrderIn>> GetListAsync(OrderInGetListQuery orderQuery);
        Task<OrderIn?> GetByIdAsync(Guid id);
    }
}
