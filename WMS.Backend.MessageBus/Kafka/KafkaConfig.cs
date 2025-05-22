namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfig
    {
        internal const string Section = "Kafka";
        public string? BootstrapServers { get; set; }
    }
}
