using WMS.Backend.Common;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInHistory : OrderIn
    {
        public Guid VersionId { get; set; }
        public DateTime VersionDateTime{ get; set; }
        public HistoryOperation Operation { get; set; }
    }
}
