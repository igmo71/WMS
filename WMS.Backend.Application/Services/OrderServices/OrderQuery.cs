namespace WMS.Backend.Application.Services.OrderServices
{
    public record OrderQuery(string? orderBy, int? Skip, int? Take);
}
