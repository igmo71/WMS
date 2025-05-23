using System.Text.Json.Serialization;

namespace WMS.Backend.Domain.Models.Catalogs
{
    internal class Zone : Catalog
    {
        public string? Code { get; set; }
        
        public Guid WarehouseId { get; set; }
        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }
    }
}
