using System;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Services
{
    internal static class ViewModelResolver
    {
        internal static ViewModelDescriptor GetMain(EntityBase entity)
        {
            return entity switch
            {
                OrderIn e => new ViewModelDescriptor($"{nameof(OrderInViewModel)}_{entity.Id}", () => new OrderInViewModel(e)),
                OrderOut e => new ViewModelDescriptor($"{nameof(OrderOutViewModel)}_{entity.Id}", () => new OrderOutViewModel(e)),
                Product e => new ViewModelDescriptor($"{nameof(ProductViewModel)}_{entity.Id}", () => new ProductViewModel(e)),
                _ => throw new NotSupportedException()
            };
        }

        internal static ViewModelDescriptor GetList(Type type)
        {
            return type switch
            {
                Type t when t == typeof(OrderIn) => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderIn)}",
                    () => new DocumentListViewModel("Order In", EntityRepositoryFactory.Get<OrderIn>())),
                Type t when t == typeof(OrderOut) => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderOut)}",
                    () => new DocumentListViewModel("Order Out", EntityRepositoryFactory.Get<OrderOut>())),
                Type t when t == typeof(Product) => new ViewModelDescriptor($"{nameof(CatalogListViewModel)}_{nameof(Product)}",
                    () => new CatalogListViewModel("Products", EntityRepositoryFactory.Get<Product>())),
                _ => throw new NotSupportedException()
            };
        }
    }

    internal readonly struct ViewModelDescriptor
    {
        public string UniqueKey { get; }
        public Func<ViewModelBase> Factory { get; }

        public ViewModelDescriptor(string uniqueKey, Func<ViewModelBase> factory)
        {
            UniqueKey = uniqueKey;
            Factory = factory;
        }
    }
}
