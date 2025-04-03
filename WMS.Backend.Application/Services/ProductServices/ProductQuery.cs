namespace WMS.Backend.Application.Services.ProductServices
{
    public record ProductQuery(string? orderBy, int? Skip, int? Take);    
}