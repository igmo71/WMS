using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Application.Services.ProductServices
{
    internal class ProductService(IProductRepository productRepository) : IProductService
    {
        private readonly ILogger _log = Log.ForContext<ProductService>();
        private readonly IProductRepository _productRepository = productRepository;

        public async Task<Product> CreateProductAsync(Product newProduct)
        {
            var product = await _productRepository.CreateAsync(newProduct);

            _log.Debug("{Source} {@Product}", nameof(CreateProductAsync), product);

            return product;
        }

        public async Task<bool> UpdateProductAsync(Guid id, Product product)
        {
            var result = await _productRepository.UpdateAsync(id, product);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(UpdateProductAsync), id, product);

            return result;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var result = await _productRepository.DeleteAsync(id);

            _log.Debug("{Source} {@ProductId}", nameof(DeleteProductAsync), id);

            return result;
        }

        public async Task<List<Product>> GetProductListAsync(ProductQuery productQuery)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {@ProductQuery}", nameof(GetProductListAsync), productQuery);

            var products = await _productRepository.GetListAsync(productQuery);

            activity.AddProperty("Products", products, destructureObjects: true);

            return products;
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(GetProductListAsync), id, product);

            return product;
        }
    }
}
