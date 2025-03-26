using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Services
{
    public class OrderService(IOrderRepository orderRepository) : IOrderService
    {
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async Task<Order> CreateAsync(Order order)
        {
            var result = await _orderRepository.CreateAsync(order);

            return result;
        }

        public async Task<int> UpdateAsync(Guid id, Order order)
        {
            var result = await _orderRepository.UpdateAsync(id, order);

            return result;
        }

        public async Task<int> DeleteAsync(Guid id)
        {
            var result = await _orderRepository.DeleteAsync(id);

            return result;
        }

        public async Task<List<Order>> GetListAsync(OrderQuery? orderQuery)
        {
            var result = await _orderRepository.GetListAsync(orderQuery);

            return result;
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            var result = await _orderRepository.GetByIdAsync(id);

            return result;
        }
    }
}
