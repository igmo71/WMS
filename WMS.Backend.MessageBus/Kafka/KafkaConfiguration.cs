namespace WMS.Backend.MessageBus.Kafka
{
    internal class KafkaConfiguration
    {
        internal const string Section = "Kafka";

        internal const string OrderInCreateCommand = "OrderInCreateCommand";
        internal const string OrderInCreatedEvent = "OrderInCreatedEvent";

        internal const string OrderInDeleteCommand = "OrderInDeleteCommand";
        internal const string OrderInDeletedEvent = "OrderInDeletedEvent";

        internal const string OrderInGetListQuery = "OrderInGetListQuery";
        internal const string OrderInGetListResponse = "OrderInGetListResponse";

        internal const string OrderInGetByIdQuery = "OrderInGetByIdQuery";
        internal const string OrderInGetByIdResponse = "OrderInGetByIdResponse";

        public string? BootstrapServers { get; set; }
    }
}
