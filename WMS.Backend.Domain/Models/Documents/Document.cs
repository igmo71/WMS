namespace WMS.Backend.Domain.Models.Documents
{
    public abstract class Document : EntityBase
    {
        public string? Name { get; set; }
        public string? Number { get; set; }
        public DateTime DateTime { get; set; }
    }
}
