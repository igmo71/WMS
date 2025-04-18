namespace WMS.Backend.Application.Services.OrderServices
{
    public record OrderInCreateCommand(string? Name, string? Number, DateTime DateTime, List<OrderInProductCreateCommand>? Products);
    public record OrderInProductCreateCommand(Guid ProductId, double Count);
}
