using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;
using WMS.Backend.Infrastructure.Data;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal class OrderInRepository(AppDbContext dbContext, IOptions<AppSettings> options) : IOrderInRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly AppSettings _appSettings = options.Value;

        public async Task<OrderIn> CreateAsync(OrderIn order)
        {
            var result = _dbContext.OrdersIn.Add(order).Entity;

            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task UpdateAsync(Guid id, OrderIn updatedOrder)
        {
            var existingOrder = await _dbContext.OrdersIn
                .Include(e => e.Products)
                .FirstOrDefaultAsync(e => e.Id == id)
                    ?? throw new ApplicationException($"Order Not Found by {id}");

            if (_appSettings.UseArchiving)
                await _dbContext.OrdersInArchive
                    .AddAsync(new OrderInArchive(existingOrder, ArchiveOperation.Update));

            _dbContext.Entry(existingOrder).CurrentValues.SetValues(updatedOrder);

            if (existingOrder.Products is not null)
                _dbContext.OrderInProducts.RemoveRange(existingOrder.Products);

            if (updatedOrder.Products is not null)
                _dbContext.OrderInProducts.AddRange(updatedOrder.Products);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var existingOrder = await _dbContext.OrdersIn
                .Include(e => e.Products)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingOrder == null)
                return;

            if (_appSettings.UseArchiving)
                await _dbContext.OrdersInArchive
                    .AddAsync(new OrderInArchive(existingOrder, ArchiveOperation.Delete));

            _dbContext.OrdersIn.Remove(existingOrder);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<OrderIn>> GetListAsync(OrderInGetListQuery orderQuery)
        {
            var result = await _dbContext.OrdersIn
                .AsNoTracking()
                .HandleQuery(orderQuery)
                .ToListAsync();

            return result;
        }

        public async Task<OrderIn?> GetByIdAsync(Guid id)
        {
            var result = await _dbContext.OrdersIn
                .AsNoTracking()
                .Include(e => e.Products)
                .FirstOrDefaultAsync(e => e.Id == id);

            return result;
        }
    }
}
