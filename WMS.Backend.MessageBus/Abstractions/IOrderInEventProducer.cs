using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Backend.Application.Services.OrderServices;

namespace WMS.Backend.MessageBus.Abstractions
{
    public interface IOrderInEventProducer
    {
        Task CreateOrderEventProduce(OrderInCreateCommand createOrderCommand);
    }
}
