using Serilog;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Application.Services
{
    public class OrderService(IOrderRepository orderRepository) : IOrderService
    {
        private readonly ILogger _log = Log.ForContext<OrderService>();
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async Task<Order> CreateAsync(Order order)
        {
            var result = await _orderRepository.CreateAsync(order);

            _log.Debug("{Source} {@Order}", nameof(CreateAsync), result);

            return result;
        }

        public async Task<int> UpdateAsync(Guid id, Order order)
        {
            var result = await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateAsync), id, order);

            return result;
        }

        public async Task<int> DeleteAsync(Guid id)
        {
            var result = await _orderRepository.DeleteAsync(id);

            _log.Debug("{Source} {@Order}", nameof(DeleteAsync), id);

            return result;
        }

        public async Task<List<Order>> GetListAsync(OrderQuery orderQuery)
        {
            using var activityListener = _log.StartActivity("{Source} {@OrderQuery}", nameof(GetListAsync), orderQuery);

            var orders = await _orderRepository.GetListAsync(orderQuery);

            activityListener.AddProperty("Orders", orders, destructureObjects: true);

            return orders;
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetListAsync), id, order);

            return order;
        }
    }
}
