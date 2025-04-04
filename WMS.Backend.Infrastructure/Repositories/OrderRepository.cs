using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;
using WMS.Backend.Infrastructure.Data;

namespace WMS.Backend.Infrastructure.Repositories
{
    public class OrderRepository(AppDbContext dbContext) : IOrderRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Order> CreateAsync(CreateOrderCommand createOrderCommand)
        {
            var newOrder = new Order
            {
                Name = createOrderCommand.Name,
                Number = createOrderCommand.Number,
                DateTime = createOrderCommand.DateTime
            };

            var result = _dbContext.Orders.Add(newOrder).Entity;

            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<bool> UpdateAsync(Guid id, Order order)
        {
            var affected = await _dbContext.Orders
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
            var affected = await _dbContext.Orders
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1;
        }

        public async Task<List<Order>> GetListAsync(OrderQuery orderQuery)
        {
            var result = await _dbContext.Orders
                .AsNoTracking()
                .HandleQuery(orderQuery)
                .ToListAsync();

            return result;
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            var result = await _dbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            return result;
        }
    }
}
