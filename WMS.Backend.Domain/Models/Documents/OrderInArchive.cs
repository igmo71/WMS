using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInArchive : EntityArchive<OrderIn>
    {
        public Guid OrderId { get; set; }

        public OrderInArchive()
        { }

        public OrderInArchive(OrderIn order, ArchiveOperation operation, string correlationId)
            : base(order, operation, correlationId)
        {
            OrderId = order.Id;
        }
    }
}
