using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using Dto = WMS.Shared.Models.Catalogs;

namespace WMS.Backend.Application.Services.ProductServices
{
    internal class ProductService(IProductRepository productRepository, IEventProducer<Dto.Product> eventProducer) : IProductService
    {
        private readonly ILogger _log = Log.ForContext<ProductService>();
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IEventProducer<Dto.Product> _eventProducer = eventProducer;

        public async Task<Dto.Product> CreateProductAsync(Dto.Product newProductDto)
        {
            var newProduct = ProductMapping.FromDto(newProductDto);

            var product = await _productRepository.CreateAsync(newProduct);

            var productDto = ProductMapping.ToDto(product);

            await _eventProducer.CreatedEventProduce(productDto);

            _log.Debug("{Source} {@Product}", nameof(CreateProductAsync), product);

            return productDto;
        }

        public async Task<bool> UpdateProductAsync(Guid id, Dto.Product productDto)
        {
            var product = ProductMapping.FromDto(productDto);

            var result = await _productRepository.UpdateAsync(id, product);

            await _eventProducer.UpdatedEventProduce(productDto);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(UpdateProductAsync), id, product);

            return result;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var result = await _productRepository.DeleteAsync(id);

            await _eventProducer.DeletedEventProduce(id);

            _log.Debug("{Source} {@ProductId}", nameof(DeleteProductAsync), id);

            return result;
        }

        public async Task<List<Dto.Product>> GetProductListAsync(ProductQuery productQuery)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {@ProductQuery}", nameof(GetProductListAsync), productQuery);

            var products = await _productRepository.GetListAsync(productQuery);

            var productDtoList = products.Select(e => ProductMapping.ToDto(e)).ToList();

            activity.AddProperty("Products", products, destructureObjects: true);

            return productDtoList;
        }

        public async Task<Dto.Product?> GetProductByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            _log.Debug("{Source} {ProductId} {@Product}", nameof(GetProductListAsync), id, product);

            if(product is null)
                return null;

            var productDto = ProductMapping.ToDto(product);

            return productDto;
        }
    }
}
