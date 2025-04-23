using System;
using System.Collections.Concurrent;
using WMS.Client.Core.Descriptors.Catalogs;
using WMS.Client.Core.Descriptors.Documents;
using WMS.Client.Core.Interfaces;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Descriptors
{
    internal class EntityDescriptorFactory
    {
        private readonly static ConcurrentDictionary<Type, IDocumentDescriptor> _documents = new();
        private readonly static ConcurrentDictionary<Type, ICatalogDescriptor> _catalogs = new();

        internal static IDocumentDescriptor GetDocument<TDocument>() where TDocument : Document => GetDocument(typeof(TDocument));

        internal static ICatalogDescriptor GetCatalog<TCatalog>() where TCatalog : Catalog => GetCatalog(typeof(TCatalog));

        internal static IDocumentDescriptor GetDocument(Type type)
        {
            if (type == typeof(OrderIn))
                return _documents.GetOrAdd(type, (t) => new OrderInDescriptor());

            if (type == typeof(OrderOut))
                return _documents.GetOrAdd(type, (t) => new OrderOutDescriptor());

            throw new NotSupportedException();
        }

        internal static ICatalogDescriptor GetCatalog(Type type)
        {
            if (type == typeof(Product))
                return _catalogs.GetOrAdd(type, (t) => new ProductDescriptor());

            throw new NotSupportedException();
        }
    }
}
