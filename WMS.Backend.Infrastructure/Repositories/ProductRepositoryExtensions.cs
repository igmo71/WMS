using WMS.Backend.Application.Services.ProductServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class ProductRepositoryExtensions
    {
        public static IQueryable<Product> HandleQuery(this IQueryable<Product> query, ProductQuery productQuery)
        {
            query = productQuery.orderBy is null
                ? query.OrderBy(e => e.Name)
                : query.OrderBy(RepoUtils.GetOrderByExpression<Product>(productQuery.orderBy));

            query = query
                .Skip(productQuery.Skip ?? AppConfig.DEFAULT_SKIP)
                .Take(productQuery.Take ?? AppConfig.DEFAULT_TAKE);

            if (!string.IsNullOrEmpty(productQuery.NameSubstring))
                query = query.Where(e => e.Name != null && e.Name.Contains(productQuery.NameSubstring));

            return query;
        }
    }
}
