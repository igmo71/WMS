using Serilog;
using Serilog.Events;
using SerilogTracing;
using WMS.Backend.Application.Abstractions.Repositories;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    internal class OrderInService(
        IOrderInRepository orderRepository,
        IOrderInProductRepository orderProductRepository) : IOrderInService
    {
        private readonly ILogger _log = Log.ForContext<OrderInService>();
        private readonly IOrderInRepository _orderRepository = orderRepository;
        private readonly IOrderInProductRepository _orderProductRepository = orderProductRepository;

        public async Task<OrderIn> CreateOrderAsync(CreateOrderInCommand createOrderCommand)
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();

            try
            {
                var order = await _orderRepository.CreateAsync(createOrderCommand);

                var productCount = await _orderProductRepository.CreateRangeAsync(order.Id, createOrderCommand.Products);

                _log.Debug("{Source} {@Order}", nameof(CreateOrderAsync), order);

                await transaction.CommitAsync();

                return order;
            }
            catch (Exception)
            {
                throw;
            }
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

            if (order is not null)
            {
                var products = await _orderProductRepository.GetListAsync(id);
                order.Products = products;
            }

            _log.Debug("{Source} {OrderId} {@Order}", nameof(GetOrderListAsync), id, order);

            return order;
        }
    }
}
