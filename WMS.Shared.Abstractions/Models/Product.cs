namespace WMS.Shared.Abstractions.Models
{
    public abstract class Product
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
