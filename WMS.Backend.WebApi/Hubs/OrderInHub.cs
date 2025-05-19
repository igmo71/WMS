using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Services;
using WMS.Backend.Application.Services.OrderInServices;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Hubs
{
    public class OrderInHub(IOrderInService orderService) : Hub<IOrderInClient>
    {
        private readonly IOrderInService _orderInService = orderService;

        public async Task<Guid> CreateOrderIn(Dto.OrderIn newOrderIn)
        {
            var orderIn = await _orderInService.CreateOrderInAsync(newOrderIn);
            await Clients.Others.OrderInCreated(orderIn);
            return orderIn.Id;
        }

        public async Task<bool> UpdateOrderIn(Dto.OrderIn orderIn)
        {
            await _orderInService.UpdateOrderInAsync(orderIn.Id, orderIn);
            await Clients.Others.OrderInUpdated(orderIn);
            return true;
        }

        public async Task<bool> DeleteOrderIn(Guid id)
        {
            await _orderInService.DeleteOrderInAsync(id);
            await Clients.Others.OrderInDeleted(id);
            return true;
        }

        public async Task<Dto.OrderIn?> GetOrderIn(Guid id)
        {
            var orderIn = await _orderInService.GetOrderAsync(id);
            return orderIn;
        }

        public async Task<List<Dto.OrderIn>> GetListOrderIn(OrderInGetListQuery orderQuery)
        {
            var orderInList = await _orderInService.GetListOrderInAsync(orderQuery);
            return orderInList;
        }
    }
}
