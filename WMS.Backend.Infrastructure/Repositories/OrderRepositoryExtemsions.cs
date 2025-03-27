using WMS.Backend.Application.Services;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Repositories
{
    public static class OrderRepositoryExtensions
    {
        public static IQueryable<Order> HandleQuery(this IQueryable<Order> query, OrderQuery? orderQuery)
        {
            if(!orderQuery.HasValue)
                return query;

            query = query
                .Skip(orderQuery.Value.Skip)
                .Take(orderQuery.Value.Take);

            return query;
        }
    }
}
