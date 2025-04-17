using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;
using WMS.Backend.Infrastructure.Data;

namespace WMS.Backend.Infrastructure.Repositories
{
    public class OrderInProductRepository(
        AppDbContext dbContext, 
        IOptions<AppSettings> options, 
        ICorrelationContext correlationContext) : IOrderInProductRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly AppSettings _appSettings = options.Value;
        private readonly ICorrelationContext _correlationContext = correlationContext;

        public async Task<int> CreateRangeAsync(Guid orderId, List<OrderInProductCreateCommand>? orderInProductCreateCommand)
        {
            if (orderInProductCreateCommand is null)
                return 0;

            var orderInProducts = orderInProductCreateCommand
                .Select(e => new OrderInProduct
                {
                    OrderId = orderId,
                    ProductId = e.ProductId,
                    Count = e.Count
                });

            await _dbContext.AddRangeAsync(orderInProducts);

            var result = await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<int> UpdateRangeAsync(Guid orderId, List<OrderInProduct>? updatedProducts)
        {
            if (updatedProducts is null)
                return 0;

            var existingProducts = await _dbContext.OrderInProducts
                .Where(e => e.OrderId == orderId)
                .ToListAsync();

            if (existingProducts is null)
                return 0;

            foreach (var existingProduct in existingProducts)
            {
                if (!updatedProducts.Any(e => e.ProductId == existingProduct.ProductId))
                {
                    _dbContext.OrderInProducts.Remove(existingProduct);
                }
            }

            foreach (var updatedProduct in updatedProducts)
            {
                var existingProduct = existingProducts
                    .FirstOrDefault(e => e.ProductId == updatedProduct.ProductId);

                if (existingProduct is null)
                {
                    updatedProduct.OrderId = orderId;
                    //existingProducts.Add(updatedProduct);
                    _dbContext.OrderInProducts.Add(updatedProduct);
                }
                else
                {
                    //_dbContext.Entry(existingProduct).CurrentValues.SetValues(updatedProduct); 
                        // Exception: The property 'OrderInProduct.OrderId' is part of a key and so cannot be modified or marked as modified.
                        // To change the principal of an existing entity with an identifying foreign key, first delete the dependent and invoke 'SaveChanges', and then associate the dependent with the new principal.
                    existingProduct.Count = updatedProduct.Count;

                }
            }

            var result = await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<int> DeleteRangeAsync(Guid orderId)
        {
            var existingProducts = await _dbContext.OrderInProducts
                .Where(e => e.OrderId == orderId)
                .ToListAsync();

            if (existingProducts is null)
                return 0;           

            _dbContext.OrderInProducts.RemoveRange(existingProducts);

            var result = await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<List<OrderInProduct>> GetListAsync(Guid orderId)
        {
            var result = await _dbContext.OrderInProducts
                .AsNoTracking()
                .Where(e => e.OrderId == orderId)
                .ToListAsync();

            return result;
        }
    }
}
