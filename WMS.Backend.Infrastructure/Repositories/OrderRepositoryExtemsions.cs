using WMS.Backend.Application.Services;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class OrderRepositoryExtensions
    {
        public static IQueryable<Order> HandleQuery(this IQueryable<Order> query, OrderQuery orderQuery)
        {
            query = query
                .Skip(orderQuery.Skip ?? AppSettings.DEFAULT_SKIP)
                .Take(orderQuery.Take ?? AppSettings.DEFAULT_TAKE);

            return query;
        }
    }
}
