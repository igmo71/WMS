using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IOrderInEventProducer orderEventProducer
        ) : IOrderInService
    {
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IOrderInEventProducer _orderEventProducer = orderEventProducer;
        private readonly ILogger _log = Log.ForContext<OrderInService>();

        public async Task<OrderIn> CreateOrderAsync(OrderIn newOrder)
        {
            var order = await _orderRepository.CreateAsync(newOrder);

            await _orderEventProducer.OrderCreatedEventProduce(order);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }

        public async Task UpdateOrderAsync(Guid id, OrderIn order)
        {
            await _orderRepository.UpdateAsync(id, order);

            await _orderEventProducer.OrderUpdatedEventProduce(order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            await _orderEventProducer.OrderDeletedEventProduce(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderAsync), id);
        }

        public async Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {@OrderQuery}", nameof(GetOrderListAsync), orderQuery);

            var orders = await _orderRepository.GetListAsync(orderQuery);

            activityListener.AddProperty("Orders", orders, destructureObjects: true);

            return orders;
        }

        public async Task<OrderIn?> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetOrderByIdAsync), id, order);

            return order;
        }
    }
}
