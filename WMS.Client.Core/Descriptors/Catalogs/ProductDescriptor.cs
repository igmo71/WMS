using System;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels.Catalogs;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.Descriptors.Catalogs
{
    internal class ProductDescriptor : ICatalogDescriptor
    {
        IEntityRepository ICatalogDescriptor.Repository => EntityRepositoryFactory.Get<Product>();

        Catalog ICatalogDescriptor.CreateNew() => new Product();

        ViewModelDescriptor ICatalogDescriptor.GetList() => new ViewModelDescriptor($"{nameof(CatalogListViewModel)}_{nameof(Product)}",
            () => new CatalogListViewModel("Products", this));

        ViewModelDescriptor ICatalogDescriptor.GetMain(Catalog catalog) => new ViewModelDescriptor($"{nameof(ProductViewModel)}_{catalog.Id}",
            () => new ProductViewModel(catalog as Product ?? throw new InvalidCastException()));

        string ICatalogDescriptor.GetUniqueKey(Catalog catalog) => $"{nameof(ProductViewModel)}_{catalog.Id}";
    }
}
