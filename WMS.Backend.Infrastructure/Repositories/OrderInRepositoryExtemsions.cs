using WMS.Backend.Application.Services.OrderInServices;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models.Documents;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class OrderInRepositoryExtensions
    {
        public static IQueryable<OrderIn> HandleQuery(this IQueryable<OrderIn> query, OrderInGetListQuery orderQuery)
        {
            query = orderQuery.OrderBy is null 
                ? query.OrderBy(e => e.DateTime) 
                : query.OrderBy(RepoUtils.GetOrderByExpression<OrderIn>(orderQuery.OrderBy));

            query = query
                .Skip(orderQuery.Skip ?? AppSettings.DEFAULT_SKIP)
                .Take(orderQuery.Take ?? AppSettings.DEFAULT_TAKE);

            if (orderQuery.DateBegin is not null)
                query = query.Where(e => e.DateTime >= orderQuery.DateBegin);
            if (orderQuery.DateEnd is not null)
                query = query.Where(e => e.DateTime < orderQuery.DateEnd);

            if (!string.IsNullOrEmpty(orderQuery.NumberSubstring))
                query = query.Where(e => e.Number != null && e.Number.Contains(orderQuery.NumberSubstring));

            return query;
        }
    }
}
