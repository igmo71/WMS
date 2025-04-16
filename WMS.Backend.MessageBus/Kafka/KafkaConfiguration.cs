namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfiguration
    {
        internal const string Section = "Kafka";

        internal const string OrderInCreated = "OrderInCreated";
        internal const string OrderInDeleted = "OrderInDeleted";

        public string? BootstrapServers { get; set; }
    }
}
