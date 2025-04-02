using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Services.ProductServices
{
    internal class ProductService(IProductRepository productRepository) : IProductService
    {
        private readonly ILogger _log = Log.ForContext<ProductService>();
        private readonly IProductRepository _productRepository = productRepository;

        public async Task<Product> CreateAsync(CreateProductCommand createCommand)
        {
            var newProduct = new Product
            {
                Name = createCommand.Name
            };

            var product = await _productRepository.CreateAsync(newProduct);

            _log.Debug("{Source} {@Product}", nameof(CreateAsync), product);

            return product;
        }

        public async Task<bool> UpdateAsync(Guid id, Product product)
        {
            var result = await _productRepository.UpdateAsync(id, product);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(UpdateAsync), id, product);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _productRepository.DeleteAsync(id);

            _log.Debug("{Source} {@ProductId}", nameof(DeleteAsync), id);

            return result;
        }

        public async Task<List<Product>> GetListAsync(ProductQuery productQuery)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {@ProductQuery}", nameof(GetListAsync), productQuery);

            var products = await _productRepository.GetListAsync(productQuery);

            activityListener.AddProperty("Products", products, destructureObjects: true);

            return products;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(GetListAsync), id, product);

            return product;
        }
    }
}
