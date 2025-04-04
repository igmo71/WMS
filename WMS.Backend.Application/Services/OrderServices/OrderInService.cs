using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(IOrderInRepository orderRepository) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;

        public async Task<OrderIn> CreateOrderAsync(CreateOrderCommand createOrderCommand)
        {
            var order = await _orderRepository.CreateAsync(createOrderCommand);

            _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

            return order;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, OrderIn order)
        {
            var result = await _orderRepository.UpdateAsync(id, order);

            _log.Debug("{Source} {IsSuccess} {OrderId} {@Order}", nameof(UpdateOrderAsync), result, id, order);

            return result;
        }

        public async Task<bool> DeleteOrderAsync(Guid id)
        {
            var result = await _orderRepository.DeleteAsync(id);

            _log.Debug("{Source} {IsSuccess} {@OrderId}", nameof(DeleteOrderAsync), result, id);

            return result;
        }

        public async Task<List<OrderIn>> GetOrderListAsync(OrderQuery orderQuery)
        {
            using var activityListener = _log.StartActivity(LogEventLevel.Debug, "{Source} {@OrderQuery}", nameof(GetOrderListAsync), orderQuery);

            var orders = await _orderRepository.GetListAsync(orderQuery);

            activityListener.AddProperty("Orders", orders, destructureObjects: true);

            return orders;
        }

        public async Task<OrderIn?> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetOrderListAsync), id, order);

            return order;
        }
    }
}
