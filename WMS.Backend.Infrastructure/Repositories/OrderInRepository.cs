﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;
using WMS.Backend.Infrastructure.Data;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal class OrderInRepository(
        AppDbContext dbContext,
        IOptions<AppSettings> options) : IOrderInRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        private readonly AppSettings _appSettings = options.Value;

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


        public async Task UpdateAsync(Guid id, OrderIn order)
        {
            var existing = await _dbContext.OrdersIn
                .FirstOrDefaultAsync(e => e.Id == id)
                    ?? throw new ApplicationException($"Order Not Found by {id}");

            if (_appSettings.UseVersioning)
                await ArchiveAsync(existing, HistoryOperation.Updated);

            _dbContext.Entry(existing).CurrentValues.SetValues(order);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var existing = await _dbContext.OrdersIn
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existing == null)
                return;

            if (_appSettings.UseVersioning)
                await ArchiveAsync(existing, HistoryOperation.Deleted);

            _dbContext.OrdersIn.Remove(existing);

            await _dbContext.SaveChangesAsync();
        }

        private async Task ArchiveAsync(OrderIn existing, HistoryOperation operation)
        {
            var archived = new OrderInHistory(existing, operation);

            await _dbContext.OrdersInHistory.AddAsync(archived);

            await _dbContext.SaveChangesAsync();
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
