namespace WMS.Shared.Abstractions.Models
{
    public abstract class Catalog
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}
