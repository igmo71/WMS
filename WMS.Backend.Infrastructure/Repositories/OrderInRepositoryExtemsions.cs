using WMS.Backend.Application.Services.OrderServices;
using WMS.Backend.Common;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class OrderInRepositoryExtensions
    {
        public static IQueryable<OrderIn> HandleQuery(this IQueryable<OrderIn> query, OrderQuery orderQuery)
        {
            query = orderQuery.orderBy is null 
                ? query.OrderBy(e => e.DateTime) 
                : query.OrderBy(RepoUtils.GetOrderByExpression<OrderIn>(orderQuery.orderBy));

            query = query
                .Skip(orderQuery.Skip ?? AppConfig.DEFAULT_SKIP)
                .Take(orderQuery.Take ?? AppConfig.DEFAULT_TAKE);

            return query;
        }
    }
}
