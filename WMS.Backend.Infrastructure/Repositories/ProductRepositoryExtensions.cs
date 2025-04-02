using WMS.Backend.Application.Services;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class ProductRepositoryExtensions
    {
        public static IQueryable<Product> HandleQuery(this IQueryable<Product> query, ProductQuery productQuery)
        {
            query = query
                .Skip(productQuery.Skip ?? AppSettings.DEFAULT_SKIP)
                .Take(productQuery.Take ?? AppSettings.DEFAULT_TAKE);

            return query;
        }
    }
}
