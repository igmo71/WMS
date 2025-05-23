using WMS.Backend.Domain.Models.Catalogs;
using Dto = WMS.Shared.Models.Catalogs;

namespace WMS.Backend.Application.Services.ProductServices
{
    internal class ProductMapping
    {
        internal static Product FromDto(Dto.Product dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = dto.Name
            };
        }

        internal static Dto.Product ToDto(Product product)
        {
            return new Dto.Product
            {
                Id = product.Id,
                Name = product.Name
            };
        }
    }
}
