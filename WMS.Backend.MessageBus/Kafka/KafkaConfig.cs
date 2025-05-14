namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfig
    {
        internal const string Section = "Kafka";
        public string? BootstrapServers { get; set; }

        // Topics
        internal const string OrderInCreated = "OrderInCreated";
        internal const string OrderInUpdated = "OrderInUpdated";
        internal const string OrderInDeleted = "OrderInDeleted";
    }
}
