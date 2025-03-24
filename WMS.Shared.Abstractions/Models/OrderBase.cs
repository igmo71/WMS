namespace WMS.Shared.Abstractions.Models
{
    public abstract class OrderBase
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Number { get; set; }
        public DateTime DateTime { get; set; }
    }
}
