namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfiguration
    {
        public const string Section = "Kafka";

        public string? BootstrapServers { get; set; }

        public Dictionary<string, string> Topics { get; set; } = [];
    }
}
