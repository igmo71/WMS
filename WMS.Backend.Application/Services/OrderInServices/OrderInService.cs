using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Serilog.Events;
using SerilogTracing;
using System.Text.Json;
using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IEventProducer<Dto.OrderIn> eventProducer,
        IDistributedCache cache) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IEventProducer<Dto.OrderIn> _eventProducer = eventProducer;
        private readonly IDistributedCache _cache = cache;

        public async Task<Dto.OrderIn> CreateOrderAsync(Dto.OrderIn newOrderDto)
        {
            var newOrder = OrderInMapping.FromDto(newOrderDto);

            var order = await _orderRepository.CreateAsync(newOrder);

            var orderDto = OrderInMapping.ToDto(order);

            SetCache(orderDto);

            await _eventProducer.CreatedEventProduce(orderDto);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return orderDto;
        }

        public async Task UpdateOrderAsync(Guid id, Dto.OrderIn orderDto)
        {
            var order = OrderInMapping.FromDto(orderDto);

            await _orderRepository.UpdateAsync(id, order);

            SetCache(orderDto);

            await _eventProducer.UpdatedEventProduce(orderDto);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            await _eventProducer.DeletedEventProduce(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderAsync), id);
        }

        public async Task<List<Dto.OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery)
        {
            var orders = await _orderRepository.GetListAsync(orderQuery);

            var orderDtoList = orders.Select(o => OrderInMapping.ToDto(o)).ToList();

            _log.Debug("{Source} {OrderQuery} {@Orders}", nameof(DeleteOrderAsync), orderQuery, orders);

            return orderDtoList;
        }

        public async Task<Dto.OrderIn?> GetOrderByIdAsync(Guid id)
        {
            using var activity = _log.StartActivity(LogEventLevel.Debug, "{Source} {OrderId}", nameof(GetOrderByIdAsync), id);

            if (TryGetFromCache(id, out Dto.OrderIn? orderDto))
                return orderDto;

            var order = await _orderRepository.GetByIdAsync(id);

            if (order is null)
                return null;

            orderDto = OrderInMapping.ToDto(order);

            SetCache(orderDto);

            activity.AddProperty("From", "Database");
            activity.AddProperty("{OrderDto}", orderDto, destructureObjects: true);

            return orderDto;
        }

        private bool TryGetFromCache(Guid id, out Dto.OrderIn? result)
        {
            var cachedBytes = cache.Get(id.ToString());

            result = null;

            if (cachedBytes is not null)
                result = JsonSerializer.Deserialize<Dto.OrderIn>(cachedBytes);

            return result is not null;
        }

        private void SetCache(Dto.OrderIn orderDto)
        {
            var cachedBytes = JsonSerializer.SerializeToUtf8Bytes(orderDto);

            cache.Set(orderDto.Id.ToString(), cachedBytes, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(60)
            });
        }
    }
}
