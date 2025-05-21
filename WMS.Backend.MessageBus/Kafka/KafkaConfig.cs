namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfig
    {
        internal const string Section = "Kafka";
        public string? BootstrapServers { get; set; }

        // Topics
        internal const string Created = "Created";
        internal const string Updated = "Updated";
        internal const string Deleted = "Deleted";
    }
}
