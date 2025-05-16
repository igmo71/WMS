using System;
using System.Collections.Concurrent;
using WMS.Client.Core.Descriptors.Catalogs;
using WMS.Client.Core.Descriptors.Documents;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Descriptors
{
    internal class EntityDescriptorFactory
    {
        private readonly static ConcurrentDictionary<Type, IEntityDescriptor> _cache = new();

        internal static IEntityDescriptor Get<TEntity>() where TEntity : EntityBase => Get(typeof(TEntity));

        internal static IEntityDescriptor Get(Type type)
        {
            if (type == typeof(OrderIn))
                return _cache.GetOrAdd(type, (t) => new OrderInDescriptor());

            if (type == typeof(OrderOut))
                return _cache.GetOrAdd(type, (t) => new OrderOutDescriptor());

            if (type == typeof(Product))
                return _cache.GetOrAdd(type, (t) => new ProductDescriptor());

            throw new NotSupportedException();
        }
    }
}
