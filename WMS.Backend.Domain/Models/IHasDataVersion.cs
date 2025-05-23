namespace WMS.Backend.Domain.Models
{
    public interface IHasDataVersion
    {
        long DataVersion { get; set; }
    }
}
