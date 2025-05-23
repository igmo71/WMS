using System.Text.Json.Serialization;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Domain.Models.Storage
{
    internal class LocationLog : VersionedEntity
    {
        public Guid LocationId { get; set; }
        [JsonIgnore]
        public StorageLocation? Location { get; set; }

        public LocationOperation Operation { get; set; }
        public double Quantity { get; set; }

        public Guid UserId { get; set; }
        [JsonIgnore]
        public AppUser? User { get; set; }

        public DateTime DateTime { get; set; }
    }
}
