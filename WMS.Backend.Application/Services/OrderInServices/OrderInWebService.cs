using WMS.Backend.Application.Abstractions.EventBus;
using WMS.Backend.Application.Abstractions.Services;
using Dto = WMS.Shared.Models.Documents;


namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInWebService(IOrderInService orderService, IOrderInEventProducer orderEventProducer) : IOrderInWebService
    {
        private readonly IOrderInService _orderService = orderService;
        private readonly IOrderInEventProducer _orderEventProducer = orderEventProducer;

        public async Task<Dto.OrderIn> CreateOrderAsync(Dto.OrderIn newOrderDto)
        {
            var newOrder = OrderInMapping.FromDto(newOrderDto);

            var order = await _orderService.CreateOrderAsync(newOrder);

            var orderDto = OrderInMapping.ToDto(order);

            await _orderEventProducer.CreatedEventProduce(orderDto);

            return orderDto;
        }

        public async Task UpdateOrderAsync(Guid id, Dto.OrderIn orderDto)
        {
            var order = OrderInMapping.FromDto(orderDto);

            await _orderService.UpdateOrderAsync(id, order);

            await _orderEventProducer.UpdatedEventProduce(orderDto);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderService.DeleteOrderAsync(id);

            await _orderEventProducer.DeletedEventProduce(id);
        }

        public async Task<List<Dto.OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery)
        {
            var orders = await _orderService.GetOrderListAsync(orderQuery);

            var orderDtoList = orders.Select(o => OrderInMapping.ToDto(o)).ToList();

            return orderDtoList;
        }

        public async Task<Dto.OrderIn?> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order is null)
                return null;

            var orderDto = OrderInMapping.ToDto(order);

            return orderDto;
        }
    }
}
