using Microsoft.AspNetCore.SignalR;
using WMS.Backend.Application.Abstractions.Services;
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

            return orderIn.Id;
        }
    }
}
