using WMS.Backend.Common;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInArchive : DocumentArchive<OrderIn>
    {
        public OrderInArchive()
        { }
        public OrderInArchive(OrderIn document, ArchiveOperation operation)
            : base(document, operation)
        { }
    }
}
