using System.Text.Json.Serialization;

namespace WMS.Backend.Domain.Models.Catalogs
{
    internal class StorageLocation : Catalog
    {
        public string? Code { get; set; }

        public Guid TypeId { get; set; }
        [JsonIgnore] 
        public StorageLocationType? Type { get; set; }

        public Guid? ZoneId { get; set; }
        [JsonIgnore]
        public Zone? Zone { get; set; }

        public StorageLocationStatus Status { get; set; }

        public double MaxWeight { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Depth { get; set; }
        public double MaxVolume => Height * Width * Depth;
        public bool IsPickable { get; set; }
        public DateTime LastInventoryDate { get; set; }
    }
}
