using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderInServices;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Hubs
{
    public class OrderInHub(IOrderInService orderService) : Hub<IOrderInClient>
    {
        private readonly IOrderInService _orderInService = orderService;

        public async Task SendOrderIn(Dto.OrderIn orderIn)
        {
            await Clients.Others.ReceiveOrderIn(orderIn);
        }

        public async Task<Guid> CreateOrderIn(Dto.OrderIn newOrderIn)
        {
            var orderIn = await _orderInService.CreateOrderAsync(newOrderIn);
            await Clients.Others.ReceiveOrderIn(orderIn);
            return orderIn.Id;
        }

        public async Task<bool> UpdateOrderIn(Dto.OrderIn orderIn)
        {
            await _orderInService.UpdateOrderAsync(orderIn.Id, orderIn);
            return true;
        }

        public async Task<bool> DeleteOrderIn(Guid id)
        {
            await _orderInService.DeleteOrderAsync(id);
            return true;
        }

        public async Task<List<Dto.OrderIn>> GetListOrderIn(OrderInGetListQuery orderQuery)
        {
            var orderInList = await _orderInService.GetOrderListAsync(orderQuery);
            return orderInList;
        }

        public async Task<Dto.OrderIn?> GetOrderIn(Guid id)
        {
            var orderIn = await _orderInService.GetOrderByIdAsync(id);
            return orderIn;
        }
    }
}
