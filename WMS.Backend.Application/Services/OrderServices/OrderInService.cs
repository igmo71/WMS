using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.MessageBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IOrderInProductRepository orderProductRepository,
        IOrderInEventProducer orderEventProducer) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IOrderInProductRepository _orderProductRepository = orderProductRepository;
        private readonly IOrderInEventProducer _orderEventProducer = orderEventProducer;

        public async Task<OrderIn> CreateOrderAsync(OrderInCreateCommand createOrderCommand, byte[]? correlationId = null)
        {
            try
            {
                await using var transaction = await _orderRepository.BeginTransactionAsync();

                var order = await _orderRepository.CreateAsync(createOrderCommand);

                var productCount = await _orderProductRepository.CreateRangeAsync(order.Id, createOrderCommand.Products);

                await transaction.CommitAsync();

                await _orderEventProducer.OrderInCreatedEventProduce(order, correlationId);

                _log.Debug("{Source} {@Order}, {CorrelationId}", nameof(CreateOrderAsync), order, correlationId);

                return order;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateOrderAsync(Guid id, OrderIn order)
        {
            await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {IsSuccess} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id, byte[]? correlationId = null)
        {
            await _orderRepository.DeleteAsync(id);

            await _orderEventProducer.OrderInDeletedEventProduce(id, correlationId);

            _log.Debug("{Source} {IsSuccess} {@OrderId}", nameof(DeleteOrderAsync), id);
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
