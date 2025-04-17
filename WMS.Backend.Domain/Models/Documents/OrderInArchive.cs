using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInArchive : EntityArchive<OrderIn>
    {
        public OrderInArchive()
        { }

        public OrderInArchive(OrderIn order, ArchiveOperation operation)
            : base(order, operation)
        { }
    }
}
