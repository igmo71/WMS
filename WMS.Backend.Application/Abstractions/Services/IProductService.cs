using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IProductService
    {
        Task<Product> CreateAsync(Product newProduct);
        Task<bool> UpdateAsync(Guid id, Product product);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Product>> GetListAsync(ProductQuery productQuery);
        Task<Product?> GetByIdAsync(Guid id);
    }
}
