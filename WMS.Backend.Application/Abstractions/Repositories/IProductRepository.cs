using WMS.Backend.Application.Services.ProductServices;
using WMS.Shared.Models.Catalogs;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IProductRepository
    {
        Task<Product> CreateAsync(Product product);
        Task<bool> UpdateAsync(Guid id, Product product);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Product>> GetListAsync(ProductQuery productQuery);
        Task<Product?> GetByIdAsync(Guid id);
    }
}
