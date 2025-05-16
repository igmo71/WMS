using System;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.Adapters.Catalogs
{
    internal abstract class CatalogAdapter : EntityAdapter
    {
        private string _name;

        internal string Name { get => LockAndGet(ref _name); set => SetAndNotify(ref _name, value); }

        internal CatalogAdapter(EntityBase entity) : base(entity)
        {
            if (entity is not Catalog)
                throw new ArgumentException();

            Catalog catalog = entity as Catalog;
            Name = catalog.Name;
        }
    }
}
