using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IOrderInProductRepository orderProductRepository,
        IOrderInEventProducer orderEventProducer
        ) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IOrderInProductRepository _orderProductRepository = orderProductRepository;
        private readonly IOrderInEventProducer _orderEventProducer = orderEventProducer;

        public async Task<OrderIn> CreateOrderAsync(OrderInCreateCommand createOrderCommand)
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();

            var order = await _orderRepository.CreateAsync(createOrderCommand);

            var productCount = await _orderProductRepository.CreateRangeAsync(order.Id, createOrderCommand.Products);

            await _orderEventProducer.OrderInCreatedEventProduce(order);

            await transaction.CommitAsync();

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }

        public async Task UpdateOrderAsync(Guid id, OrderIn order)
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();

            await _orderRepository.UpdateAsync(id, order);

            await _orderProductRepository.UpdateRangeAsync(id, order.Products);

            await _orderEventProducer.OrderInUpdatedEventProduce(order);

            await transaction.CommitAsync();

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();

            await _orderProductRepository.DeleteRangeAsync(id);

            await _orderRepository.DeleteAsync(id);

            await _orderEventProducer.OrderInDeletedEventProduce(id);

            await transaction.CommitAsync();

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

            if (order is not null)
            {
                var products = await _orderProductRepository.GetListAsync(id);
                order.Products = products;
            }

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetOrderByIdAsync), id, order);

            return order;
        }
    }
}
