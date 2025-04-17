using System.Text.Json;
using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models
{
    public abstract class EntityArchive<T> : EntityBase
    {
        public string CorrelationId { get; set; }
        public DateTime DateTime { get; set; }
        public ArchiveOperation Operation { get; set; }
        public string? Archive { get; set; }

        protected EntityArchive() { }

        protected EntityArchive(T archive, ArchiveOperation operation, string correlationId)
        {
            CorrelationId = correlationId;
            DateTime = DateTime.UtcNow;
            Operation = operation;
            Archive = JsonSerializer.Serialize(archive, AppConfig.JsonSerializerOptions);
        }
    }
}
