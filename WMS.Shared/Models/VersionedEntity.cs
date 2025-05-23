namespace WMS.Shared.Models
{
    public abstract class VersionedEntity : EntityBase
    {
        public long DataVersion { get; set; }
    }
}
