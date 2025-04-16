namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfiguration
    {
        internal const string Section = "Kafka";
        public string? BootstrapServers { get; set; }

        internal const string OrderInCreated = "OrderInCreated";
        internal const string OrderInDeleted = "OrderInDeleted";
    }
}
