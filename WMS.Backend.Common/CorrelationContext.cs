namespace WMS.Backend.Common
{
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
