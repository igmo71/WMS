using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    public class OrderService(IOrderRepository orderRepository) : IOrderService
    {
        private readonly ILogger _log = Log.ForContext<OrderService>();
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async Task<Order> CreateOrderAsync(CreateOrderCommand createCommand)
        {
            var newOrder = new Order
            {
                Name = createCommand.Name,
                Number = createCommand.Number,
                DateTime = createCommand.DateTime
            };

            var order = await _orderRepository.CreateAsync(newOrder);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, Order order)
        {
            var result = await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);

            return result;
        }

        public async Task<bool> DeleteOrderAsync(Guid id)
        {
            var result = await _orderRepository.DeleteAsync(id);

            _log.Debug("{Source} {@OrderId}", nameof(DeleteOrderAsync), id);

            return result;
        }

        public async Task<List<Order>> GetOrderListAsync(OrderQuery orderQuery)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {@OrderQuery}", nameof(GetOrderListAsync), orderQuery);

            var orders = await _orderRepository.GetListAsync(orderQuery);

            activityListener.AddProperty("Orders", orders, destructureObjects: true);

            return orders;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetOrderListAsync), id, order);

            return order;
        }
    }
}
