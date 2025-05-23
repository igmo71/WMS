namespace WMS.Backend.Domain.Models.Catalogs
{
    public abstract class Catalog : VersionedEntity
    {
        public string? Name { get; set; }
    }
}
