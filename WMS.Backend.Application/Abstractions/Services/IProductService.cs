using WMS.Backend.Application.Services.ProductServices;
using Dto = WMS.Shared.Models.Catalogs;

namespace WMS.Backend.Application.Abstractions.Services
{
    public interface IProductService
    {
        Task<Dto.Product> CreateProductAsync(Dto.Product newProduct);
        Task<bool> UpdateProductAsync(Guid id, Dto.Product product);
        Task<bool> DeleteProductAsync(Guid id);
        Task<Dto.Product?> GetProductAsync(Guid id);
        Task<List<Dto.Product>> GetListProductAsync(ProductQuery productQuery);
    }
}
