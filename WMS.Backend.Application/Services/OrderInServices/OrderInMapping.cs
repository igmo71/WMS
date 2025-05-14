using WMS.Backend.Domain.Models.Documents;
using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInMapping
    {
        internal static OrderIn FromDto(Dto.OrderIn dto)
        {
            return new OrderIn
            {
                DateTime = dto.DateTime,
                Id = dto.Id,
                Name = dto.Name,
                Number = dto.Number,
                Products = dto.Products?
                    .Select(p => new OrderInProduct
                    {
                        OrderId = dto.Id,
                        ProductId = p.ProductId,
                        Count = p.Count,
                    })
                    .ToList()
            };
        }

        internal static Dto.OrderIn ToDto(OrderIn order)
        {
            return new Dto.OrderIn
            {
                DateTime = order.DateTime,
                Id = order.Id,
                Name = order.Name,
                Number = order.Number,
                Products = order.Products?
                    .Select(p => new Dto.OrderIn.OrderInProduct
                    {
                        ProductId = p.ProductId,
                        Count = p.Count,
                    })
                    .ToList()
            };
        }
    }
}
