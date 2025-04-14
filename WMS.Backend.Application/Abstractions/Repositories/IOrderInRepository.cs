﻿using Microsoft.EntityFrameworkCore.Storage;
using WMS.Backend.Application.Services.OrderServices;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderInRepository
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<OrderIn> CreateAsync(OrderInCreateCommand createOrderCommand);
        Task UpdateAsync(Guid id, OrderIn order);
        Task DeleteAsync(Guid id);
        Task<List<OrderIn>> GetListAsync(OrderQuery orderQuery);
        Task<OrderIn?> GetByIdAsync(Guid id);
    }
}
