using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Infrastructure.Data;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal class OrderInRepositoryVersioning(AppDbContext dbContext) : IOrderInRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task<OrderIn> CreateAsync(CreateOrderInCommand createOrderCommand)
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


        public async Task<bool> UpdateAsync(Guid id, OrderIn order)
        {
            var affected = await _dbContext.OrdersIn
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Id, order.Id)
                    .SetProperty(m => m.Name, order.Name)
                    .SetProperty(m => m.Number, order.Number)
                    .SetProperty(m => m.DateTime, order.DateTime));

            return affected == 1;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var affected = await _dbContext.OrdersIn
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1;
        }

        public async Task<List<OrderIn>> GetListAsync(OrderQuery orderQuery)
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
