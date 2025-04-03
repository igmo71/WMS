using System.Linq.Expressions;

namespace WMS.Backend.Infrastructure.Repositories
{
    internal static class RepoUtils
    {
        public static Expression<Func<T, object>> GetOrderByExpression<T>(string orderBy)
        {
            var param = Expression.Parameter(typeof(T), "e");
            var property = Expression.Property(param, orderBy);
            var converted = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(converted, param);
        }
    }
}
