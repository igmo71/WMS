namespace WMS.Backend.Application.Services.OrderServices
{
    public record CreateOrderCommand(string? Name, string? Number, DateTime DateTime);
}
