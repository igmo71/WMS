using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Documents;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(IOrderInRepository orderRepository, IOrderInEventProducer orderEventProducer) : IOrderInService
    {
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IOrderInEventProducer _orderEventProducer = orderEventProducer;
        private readonly ILogger _log = Log.ForContext<OrderInService>();

        public async Task<Dto.OrderIn> CreateOrderByDtoAsync(Dto.OrderIn newOrderDto)
        {
            var newOrder = OrderInMapping.FromDto(newOrderDto);

            var order = await CreateOrderAsync(newOrder);

            var orderDto = OrderInMapping.ToDto(order);

            await _orderEventProducer.OrderCreatedEventProduce(orderDto);

            return orderDto;
        }

        public async Task<OrderIn> CreateOrderAsync(OrderIn newOrder)
        {
            var order = await _orderRepository.CreateAsync(newOrder);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }

        public async Task UpdateOrderByDtoAsync(Guid id, Dto.OrderIn orderDto)
        {
            var order = OrderInMapping.FromDto(orderDto);

            await UpdateOrderAsync(id, order);

            await _orderEventProducer.OrderUpdatedEventProduce(orderDto);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task UpdateOrderAsync(Guid id, OrderIn order)
        {
            await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            await _orderEventProducer.OrderDeletedEventProduce(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderAsync), id);
        }

        public async Task<List<Dto.OrderIn>> GetOrderDtoListAsync(OrderInGetListQuery orderQuery)
        {
            var orders = await GetOrderListAsync(orderQuery);

            var orderDtoList = orders.Select(o => OrderInMapping.ToDto(o)).ToList();

            return orderDtoList;
        }

        public async Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {@OrderQuery}", nameof(GetOrderListAsync), orderQuery);

            var orders = await _orderRepository.GetListAsync(orderQuery);

            activity.AddProperty("Orders", orders, destructureObjects: true);

            return orders;
        }

        public async Task<Dto.OrderIn?> GetOrderDtoByIdAsync(Guid id)
        {
            var order = await GetOrderByIdAsync(id);

            var orderDto = OrderInMapping.ToDto(order);

            return orderDto;
        }

        public async Task<OrderIn?> GetOrderByIdAsync(Guid id)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {OrderId}", nameof(GetOrderByIdAsync), id);

            var order = await _orderRepository.GetByIdAsync(id);

            activity.AddProperty("Order", order, destructureObjects: true);

            return order;
        }
    }
}
