using Serilog;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInService(IOrderInRepository orderRepository) : IOrderInService
    {
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly ILogger _log = Log.ForContext<OrderInService>();

        

        public async Task<OrderIn> CreateOrderAsync(OrderIn newOrder)
        {
            var order = await _orderRepository.CreateAsync(newOrder);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }        

        public async Task UpdateOrderAsync(Guid id, OrderIn order)
        {
            await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(UpdateOrderAsync), id, order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);

            _log.Debug("{Source} {OrderId}", nameof(DeleteOrderAsync), id);
        }
        
        public async Task<List<OrderIn>> GetOrderListAsync(OrderInGetListQuery orderQuery)
        {
            var orders = await _orderRepository.GetListAsync(orderQuery);

            _log.Debug("{Source} {Orders}", nameof(DeleteOrderAsync), orders);

            return orders;
        }

        public async Task<OrderIn?> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            _log.Debug("{Source} {Order}", nameof(GetOrderByIdAsync), order);

            return order;
        }
    }
}
