using System.Text.Json;
using WMS.Backend.Common;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Backend.Domain.Models.Documents
{
    public abstract class DocumentArchive<TDocument> : EntityBase where TDocument : Document
    {
        public DateTime DateTime { get; set; }
        public ArchiveOperation Operation { get; set; }
        public Guid DocumentId { get; set; }
        public string? Document { get; set; }

        protected DocumentArchive() { }

        protected DocumentArchive(TDocument document, ArchiveOperation operation)
        {
            DateTime = DateTime.UtcNow;
            Operation = operation;
            DocumentId = document.Id;
            Document = JsonSerializer.Serialize(document);
        }
    }
}
