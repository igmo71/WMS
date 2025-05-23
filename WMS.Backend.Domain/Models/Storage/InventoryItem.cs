using System.Text.Json.Serialization;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Domain.Models.Storage
{
    internal class InventoryItem : VersionedEntity
    {
        public Guid LocationId { get; set; }
        [JsonIgnore]
        public StorageLocation? Location { get; set; }

        public Guid ProductId { get; set; }
        [JsonIgnore]
        public Product? Product { get; set; }

        public double Quantity { get; set; }
        public string? Batch { get; set; }
        public DateTime ExpiryDate { get; set; }
        public InventoryItemStatus Status { get; set; }
    }
}
