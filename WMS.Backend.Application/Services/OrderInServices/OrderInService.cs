using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Cache;
//using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Hubs;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        //IEventProducer<Dto.OrderIn> eventProducer,
        IAppHubService orderInHub,
        IAppCache cache) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        //private readonly IEventProducer<Dto.OrderIn> _eventProducer = eventProducer;
        private readonly IAppHubService _orderInHub = orderInHub;
        private readonly IAppCache _cache = cache;

        public async Task<Dto.OrderIn> CreateOrderInAsync(Dto.OrderIn newOrderDto)
        {
            var newOrder = OrderInMapping.FromDto(newOrderDto);

            var order = await _orderRepository.CreateAsync(newOrder);

            var orderDto = OrderInMapping.ToDto(order);

            await _cache.SetAsync(orderDto);

            //await _eventProducer.CreatedEventProduce(orderDto);
            await _orderInHub.CreatedAsync(orderDto);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderInAsync), order);

            return orderDto;
        }

        public async Task UpdateOrderInAsync(Guid id, Dto.OrderIn orderDto)
        {
            var order = OrderInMapping.FromDto(orderDto);

            await _orderRepository.UpdateAsync(id, order);

            await _cache.SetAsync(orderDto);

            //await _eventProducer.UpdatedEventProduce(orderDto);
            await _orderInHub.UpdatedAsync(orderDto);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderInAsync), id, order);
        }

        public async Task DeleteOrderInAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            //await _eventProducer.DeletedEventProduce(id);
            await _orderInHub.DeletedAsync(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderInAsync), id);
        }

        public async Task<Dto.OrderIn?> GetOrderAsync(Guid id)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {OrderId}", nameof(GetOrderAsync), id);

            var orderDto = await _cache.GetAsync<Dto.OrderIn>(id);
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
