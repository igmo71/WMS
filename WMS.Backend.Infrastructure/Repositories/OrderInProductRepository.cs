using Microsoft.EntityFrameworkCore;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Infrastructure.Data;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Infrastructure.Repositories
{
    public class OrderInProductRepository(AppDbContext dbContext) : IOrderInProductRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<int> CreateRangeAsync(Guid orderId, List<CreateOrderInProduct>? createOrderInProducts)
        {
            if(createOrderInProducts is null)
                return 0;

            var orderInProducts = createOrderInProducts
                .Select(e => new OrderInProduct { OrderId = orderId, ProductId = e.ProductId, Count = e.Count });

            await _dbContext.AddRangeAsync(orderInProducts);

            var result = await _dbContext.SaveChangesAsync();

            return result;
        }

        public Task<List<OrderInProduct>> GetListAsync(Guid orderId)
        {
            var result = _dbContext.OrderInProducts
                .AsNoTracking()
                .Where(e => e.OrderId == orderId)
                .ToListAsync();

            return result;
        }
    }
}
