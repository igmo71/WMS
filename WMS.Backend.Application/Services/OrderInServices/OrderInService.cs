using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Cache;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Documents;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IAppCache<Dto.OrderIn> cache,
        OrderInEventBus eventBus) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IAppCache<Dto.OrderIn> _cache = cache;
        private readonly IEventProducer<OrderIn> _eventBus = eventBus;

        public async Task<Dto.OrderIn> CreateOrderInAsync(Dto.OrderIn newOrderDto)
        {
            var newOrder = OrderInMapping.FromDto(newOrderDto);

            var order = await _orderRepository.CreateAsync(newOrder);

            var orderDto = OrderInMapping.ToDto(order);

            await _cache.SetAsync(orderDto);

            await _eventBus.CreatedEventProduce(order);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderInAsync), order);

            return orderDto;
        }

        public async Task UpdateOrderInAsync(Guid id, Dto.OrderIn orderDto)
        {
            var order = OrderInMapping.FromDto(orderDto);

            await _orderRepository.UpdateAsync(id, order);

            await _cache.SetAsync(orderDto);

            await _eventBus.UpdatedEventProduce(order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderInAsync), id, order);
        }

        public async Task DeleteOrderInAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            await _cache.RemoveAsync(id);

            await _eventBus.DeletedEventProduce(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderInAsync), id);
        }

        public async Task<Dto.OrderIn?> GetOrderAsync(Guid id)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {OrderId}", nameof(GetOrderAsync), id);

            var orderDto = await _cache.GetAsync(id);
            if (orderDto is not null)
                return orderDto;

            var order = await _orderRepository.GetAsync(id);

            if (order is null)
                return null;

            orderDto = OrderInMapping.ToDto(order);

            await _cache.SetAsync(orderDto);

            activity.AddProperty("{OrderDto}", orderDto, destructureObjects: true);

            return orderDto;
        }

        public async Task<List<Dto.OrderIn>> GetListOrderInAsync(OrderInGetListQuery orderQuery)
        {
            var orders = await _orderRepository.GetListAsync(orderQuery);

            var orderDtoList = orders.Select(o => OrderInMapping.ToDto(o)).ToList();

            _log.Debug("{Source} {OrderQuery} {@Orders}", nameof(DeleteOrderInAsync), orderQuery, orders);

            return orderDtoList;
        }
    }
}
