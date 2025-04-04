using WMS.Shared.Models.Documents;

namespace WMS.Backend.Application.Services.OrderServices
{
    public record CreateOrderCommand(string? Name, string? Number, DateTime DateTime, List<CreateOrderInProduct>? Products);
}
