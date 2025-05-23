namespace WMS.Backend.Domain.Models.Catalogs
{
    internal class Warehouse : Catalog
    {
        public string? Address { get; set; }

        public List<Zone>? Zones { get; set; }
    }
}
