using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class OrderRepositoryExtensions
    {
        public static IQueryable<Order> HandleQuery(this IQueryable<Order> query, OrderQuery orderQuery)
        {
            query = orderQuery.orderBy is null 
                ? query.OrderBy(e => e.DateTime) 
                : query.OrderBy(RepoUtils.GetOrderByExpression<Order>(orderQuery.orderBy));

            query = query
                .Skip(orderQuery.Skip ?? AppSettings.DEFAULT_SKIP)
                .Take(orderQuery.Take ?? AppSettings.DEFAULT_TAKE);

            return query;
        }
    }
}
