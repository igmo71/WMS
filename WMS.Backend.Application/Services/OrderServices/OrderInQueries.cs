namespace WMS.Backend.Application.Services.OrderServices
{
    public record OrderInGetListQuery(string? orderBy, int? Skip, int? Take);
}
