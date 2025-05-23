namespace WMS.Backend.Domain.Models
{
    public class VersionedEntity : EntityBase, IHasDataVersion
    {
        public long DataVersion { get; set; }
    }
}
