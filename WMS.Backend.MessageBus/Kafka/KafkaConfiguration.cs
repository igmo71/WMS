namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfiguration
    {
        public const string Section = "Kafka";

        public const string OrderInCreateCommand = "OrderInCreateCommand";
        public const string OrderInCreatedEvent = "OrderInCreatedEvent";

        public const string OrderInDeleteCommand = "OrderInDeleteCommand";
        public const string OrderInDeletedEvent = "OrderInDeletedEvent";

        public const string OrderInGetListQuery = "OrderInGetListQuery";
        public const string OrderInGetListResponse = "OrderInGetListResponse";

        public string? BootstrapServers { get; set; }
    }
}
