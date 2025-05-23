namespace WMS.Shared.Models
{
    public abstract class EntityBase
    {
        public Guid Id { get; set; }
        public long DataVersion { get; set; }
    }
}
