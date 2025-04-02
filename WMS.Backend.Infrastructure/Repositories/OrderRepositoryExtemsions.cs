using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Repositories
{
    public static class OrderRepositoryExtensions
    {
        public static IQueryable<Order> HandleQuery(this IQueryable<Order> query, OrderQuery orderQuery)
        {
            query = query
                .Skip(orderQuery.Skip ?? 0)
                .Take(orderQuery.Take ?? 100);

            return query;
        }
    }
}
