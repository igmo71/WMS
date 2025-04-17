using System.Text.Json;
using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models
{
    public abstract class EntityArchive<T> : EntityBase where T : EntityBase
    {
        public DateTime DateTime { get; set; }
        public ArchiveOperation Operation { get; set; }
        public Guid ArchivelId { get; set; }
        public string? Archive { get; set; }

        protected EntityArchive() { }

        protected EntityArchive(T archive, ArchiveOperation operation)
        {
            DateTime = DateTime.UtcNow;
            Operation = operation;
            ArchivelId = archive.Id;
            Archive = JsonSerializer.Serialize(archive, AppConfig.JsonSerializerOptions);
        }
    }
}
