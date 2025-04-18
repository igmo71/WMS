namespace WMS.Backend.Application.Services.OrderServices
{
    public record OrderInGetListQuery(
        string? OrderBy, 
        int? Skip, 
        int? Take, 
        DateTime? DateBegin, 
        DateTime? DateEnd, 
        string? NumberSubstring);
}
