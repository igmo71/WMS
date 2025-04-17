using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;
using WMS.Backend.Infrastructure.Data;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal class OrderInRepository(
        AppDbContext dbContext, 
        IOptions<AppSettings> options,
        ICorrelationContext correlationContext) : IOrderInRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly AppSettings _appSettings = options.Value;
        private readonly ICorrelationContext _correlationContext = correlationContext;

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task<OrderIn> CreateAsync(OrderInCreateCommand createOrderCommand)
        {
            var newOrder = new OrderIn
            {
                Name = createOrderCommand.Name,
                Number = createOrderCommand.Number,
                DateTime = createOrderCommand.DateTime
            };

            var result = _dbContext.OrdersIn.Add(newOrder).Entity;

            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task UpdateAsync(Guid id, OrderIn updatedOrder)
        {
            var existingOrder = await _dbContext.OrdersIn
                .FirstOrDefaultAsync(e => e.Id == id)
                    ?? throw new ApplicationException($"Order Not Found by {id}");

            if (_appSettings.UseArchiving)
            {
                var archivedOrder = new OrderInArchive(existingOrder, ArchiveOperation.Update, _correlationContext.CorrelationId);

                await _dbContext.OrdersInArchive.AddAsync(archivedOrder);
            }

            _dbContext.Entry(existingOrder).CurrentValues.SetValues(updatedOrder);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var existingOrder = await _dbContext.OrdersIn
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingOrder == null)
                return;

            if (_appSettings.UseArchiving)
            {
                var archivedOrder = new OrderInArchive(existingOrder, ArchiveOperation.Delete, _correlationContext.CorrelationId);

                await _dbContext.OrdersInArchive.AddAsync(archivedOrder);
            }

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
                .FirstOrDefaultAsync(e => e.Id == id);

            return result;
        }
    }
}
