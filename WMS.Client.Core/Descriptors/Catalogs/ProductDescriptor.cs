using System;
using WMS.Client.Core.Adapters;
using WMS.Client.Core.Adapters.Catalogs;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels.Catalogs;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.Descriptors.Catalogs
{
    internal class ProductDescriptor : IEntityDescriptor
    {
        IEntityRepository IEntityDescriptor.Repository => EntityRepositoryFactory.Get<Product>();

        EntityAdapter IEntityDescriptor.CreateNew() => new ProductAdapter(new Product());

        EntityAdapter IEntityDescriptor.GetAdapter(EntityBase entity) => new ProductAdapter(entity);

        ViewModelDescriptor IEntityDescriptor.GetList() => new ViewModelDescriptor($"{nameof(CatalogListViewModel)}_{nameof(Product)}",
            () => new CatalogListViewModel("Products", this));

        ViewModelDescriptor IEntityDescriptor.GetMain(EntityAdapter adapter) => new ViewModelDescriptor($"{nameof(ProductViewModel)}_{adapter.Id}",
            () => new ProductViewModel(adapter as ProductAdapter ?? throw new InvalidCastException()));

        string IEntityDescriptor.GetUniqueKey(EntityAdapter adapter) => $"{nameof(ProductViewModel)}_{adapter.Id}";
    }
}
