using System.Text.Json;
using WMS.Backend.Common;

namespace WMS.Backend.Domain.Models
{
    public abstract class EntityArchive<T> : EntityBase where T : EntityBase
    {
        public DateTime DateTime { get; set; }
        public ArchiveOperation Operation { get; set; }
        public Guid ArchiveId { get; set; }
        public string? Archive { get; set; }

        protected EntityArchive() { }

        protected EntityArchive(T archive, ArchiveOperation operation)
        {
            DateTime = DateTime.Now;
            Operation = operation;
            ArchiveId = archive.Id;
            Archive = JsonSerializer.Serialize(archive, AppSettings.JsonSerializerOptions);
        }
    }
}
