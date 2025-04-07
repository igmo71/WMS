namespace WMS.Backend.Application.Services.OrderServices
{
    public record CreateOrderInCommand(string? Name, string? Number, DateTime DateTime, List<CreateOrderInProduct>? Products);
    public record CreateOrderInProduct(Guid ProductId, double Count);
}
