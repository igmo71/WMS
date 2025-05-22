using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Cache;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using Dto = WMS.Shared.Models.Catalogs;

namespace WMS.Backend.Application.Services.ProductServices
{
    internal class ProductService(
        IProductRepository productRepository,
        IAppHubService<Dto.Product> eventHub,
        IAppCache<Dto.Product> cache) : IProductService
    {
        private readonly ILogger _log = Log.ForContext<ProductService>();
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IAppHubService<Dto.Product> _eventHub = eventHub;
        private readonly IAppCache<Dto.Product> _cache = cache;

        public async Task<Dto.Product> CreateProductAsync(Dto.Product newProductDto)
        {
            var newProduct = ProductMapping.FromDto(newProductDto);

            var product = await _productRepository.CreateAsync(newProduct);

            var productDto = ProductMapping.ToDto(product);

            await _cache.SetAsync(productDto);

            await _eventHub.CreatedAsync(productDto);

            _log.Debug("{Source} {@Product}", nameof(CreateProductAsync), product);

            return productDto;
        }

        public async Task<bool> UpdateProductAsync(Guid id, Dto.Product productDto)
        {
            var product = ProductMapping.FromDto(productDto);

            var result = await _productRepository.UpdateAsync(id, product);

            await _cache.SetAsync(productDto);

            await _eventHub.UpdatedAsync(productDto);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(UpdateProductAsync), id, product);

            return result;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var result = await _productRepository.DeleteAsync(id);

            await _cache.RemoveAsync(id);

            await _eventHub.DeletedAsync(id);

            _log.Debug("{Source} {ProductId}", nameof(DeleteProductAsync), id);

            return result;
        }

        public async Task<Dto.Product?> GetProductAsync(Guid id)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {ProductId}", nameof(GetProductAsync), id);

            var productDto = await _cache.GetAsync(id);
            if (productDto is not null)
                return productDto;

            var product = await _productRepository.GetAsync(id);

            if (product is null)
                return null;

            productDto = ProductMapping.ToDto(product);

            await _cache.SetAsync(productDto);

            activity.AddProperty("ProductDto", productDto, destructureObjects: true);

            return productDto;
        }

        public async Task<List<Dto.Product>> GetListProductAsync(ProductQuery productQuery)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {@ProductQuery}", nameof(GetListProductAsync), productQuery);

            var products = await _productRepository.GetListAsync(productQuery);

            var productDtoList = products.Select(e => ProductMapping.ToDto(e)).ToList();

            activity.AddProperty("Products", products, destructureObjects: true);

            return productDtoList;
        }
    }
}
