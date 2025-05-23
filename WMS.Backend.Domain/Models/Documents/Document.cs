namespace WMS.Backend.Domain.Models.Documents
{
    public abstract class Document : VersionedEntity
    {
        public string? Name { get; set; }
        public string? Number { get; set; }
        public DateTime DateTime { get; set; }
    }
}
