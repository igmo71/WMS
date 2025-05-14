namespace WMS.Backend.Common
{
    // TODO: CorrelationId Не используется сейчас
    public class CorrelationContext : ICorrelationContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new();

        public string CorrelationId
        {
            get => _correlationId.Value ?? string.Empty;
            set => _correlationId.Value = value;
        }
    }
}
