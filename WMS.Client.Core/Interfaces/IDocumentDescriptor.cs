using WMS.Client.Core.Infrastructure;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Interfaces
{
    internal interface IDocumentDescriptor
    {
        internal IEntityRepository Repository { get; }

        internal Document CreateNew();
        internal ViewModelDescriptor GetMain(Document document);
        internal ViewModelDescriptor GetList();
        internal string GetUniqueKey(Document document);
    }
}
