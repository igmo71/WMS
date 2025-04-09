using WMS.Backend.Common;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInHistory : DocumentHistory<OrderIn>
    {
        public OrderInHistory()
        { }
        public OrderInHistory(OrderIn document, HistoryOperation operation)
            : base(document, operation)
        { }
    }
}
