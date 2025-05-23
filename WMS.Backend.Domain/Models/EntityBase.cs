namespace WMS.Backend.Domain.Models
{
    public abstract class EntityBase : IHasDataVersion
    {
        public Guid Id { get; set; }
        public long DataVersion { get; set; }
    }
}
