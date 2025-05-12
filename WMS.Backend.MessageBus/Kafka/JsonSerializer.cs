using Confluent.Kafka;
using System.Text.Json;
using WMS.Backend.Common;

namespace WMS.Backend.MessageBus.Kafka
{
    internal class JsonSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T data, SerializationContext context) => 
            JsonSerializer.SerializeToUtf8Bytes(data, AppSettings.JsonSerializerOptions);
    }
}
