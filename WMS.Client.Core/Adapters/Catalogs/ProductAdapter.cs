using System;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.Adapters.Catalogs
{
    internal class ProductAdapter : CatalogAdapter
    {
        internal ProductAdapter(EntityBase entity) : base(entity)
        {
            if (entity is not Product)
                throw new ArgumentException();

            EntityRepositoryFactory.Get<Product>().EntityUpdated += EntityUpdated;
        }

        private void EntityUpdated(object? sender, EntityChangedEventArgs e)
        {
            if (e.Entity is Product product && product.Id == Id)
                UpdateAdapter(product);
        }

        private void UpdateAdapter(Product product)
        {
            Name = product.Name;
        }

        internal override void Save() => _id = EntityRepositoryFactory.Get<Product>().CreateOrUpdate(this);

        internal override EntityBase GetEntity() => new Product() { Id = Id, Name = Name };

    }
}
