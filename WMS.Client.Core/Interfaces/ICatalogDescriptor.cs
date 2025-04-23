using WMS.Client.Core.Infrastructure;
using WMS.Shared.Models.Catalogs;

namespace WMS.Client.Core.Interfaces
{
    internal interface ICatalogDescriptor
    {
        internal IEntityRepository Repository { get; }

        internal Catalog CreateNew();
        internal ViewModelDescriptor GetMain(Catalog catalog);
        internal ViewModelDescriptor GetList();
        internal string GetUniqueKey(Catalog catalog);
    }
}
