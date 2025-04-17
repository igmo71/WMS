using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInProductArchive : EntityArchive<OrderInProduct>
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }

        public OrderInProductArchive()
        { }

        public OrderInProductArchive(OrderInProduct orderProduct, ArchiveOperation operation, string correlationId)
            : base(orderProduct, operation, correlationId)
        {
            OrderId = orderProduct.OrderId;
            ProductId = orderProduct.ProductId;
        }
    }
}
