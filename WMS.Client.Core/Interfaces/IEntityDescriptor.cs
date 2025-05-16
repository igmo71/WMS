using WMS.Client.Core.Adapters;
using WMS.Client.Core.Infrastructure;
using WMS.Shared.Models;

namespace WMS.Client.Core.Interfaces
{
    internal interface IEntityDescriptor
    {
        internal IEntityRepository Repository { get; }

        internal EntityAdapter CreateNew();
        internal EntityAdapter GetAdapter(EntityBase entity);
        internal ViewModelDescriptor GetMain(EntityAdapter adapter);
        internal ViewModelDescriptor GetList();
        internal string GetUniqueKey(EntityAdapter adapter);
    }
}
