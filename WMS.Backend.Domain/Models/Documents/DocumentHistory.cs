using System.Text.Json;
using WMS.Backend.Common;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Domain.Models.Documents
{
    public abstract class DocumentHistory<TDocument> where TDocument : Document
    {
        public Guid VersionId { get; set; }
        public Guid DocumentId { get; set; }
        public DateTime VersionDateTime { get; set; }
        public HistoryOperation Operation { get; set; }
        public string? Document { get; set; }

        protected DocumentHistory() { }

        protected DocumentHistory(TDocument document, HistoryOperation operation)
        {
            DocumentId = document.Id;
            Document = JsonSerializer.Serialize(document);
            VersionDateTime = DateTime.UtcNow;
            Operation = operation;
        }
    }
}
