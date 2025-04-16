namespace WMS.Backend.Common
{
    public interface ICorrelationContext
    {
        public string CorrelationId { get; set; }
    }
}
