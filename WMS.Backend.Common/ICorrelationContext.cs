namespace WMS.Backend.Common
{
    // TODO: CorrelationId Не используется сейчас
    public interface ICorrelationContext
    {
        public string CorrelationId { get; set; }
    }
}
