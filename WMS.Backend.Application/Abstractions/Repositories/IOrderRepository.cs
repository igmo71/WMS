﻿using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Abstractions.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(CreateOrderCommand createOrderCommand);
        Task<bool> UpdateAsync(Guid id, Order order);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Order>> GetListAsync(OrderQuery orderQuery);
        Task<Order?> GetByIdAsync(Guid id);
    }
}
