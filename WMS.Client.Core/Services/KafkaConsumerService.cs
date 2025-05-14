using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace WMS.Client.Core.Services
{
    internal class KafkaConsumerService : IDisposable
    {
        private CancellationTokenSource _cts;
        private IConsumer<Ignore, string> _consumer;
        private Task _loopTask;

        public event EventHandler<KafkaMessageConsumedEventArgs> MessageConsumed;

        public KafkaConsumerService()
        {
            var config = new ConsumerConfig()
            {
                BootstrapServers = "igmo-pc:29092, igmo-pc:39092, igmo-pc:49092",
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            _consumer.Subscribe("OrderInCreated");
            _consumer.Subscribe("OrderInDeleted");
            _consumer.Subscribe("OrderInUpdated");

            _loopTask = Task.Run(Loop);
        }

        private void Loop()
        {
            _cts = new CancellationTokenSource();
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    ConsumeResult<Ignore, string> result;

                    try
                    {
                        result = _consumer.Consume(_cts.Token);
                    }
                    catch (OperationCanceledException ex)
                    {
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        continue;
                    }

                    MessageConsumed?.Invoke(null, new KafkaMessageConsumedEventArgs(result.Topic, result.Message.Value));
                }
            }
            finally
            {
                _consumer.Close();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _loopTask.Wait();

            _cts.Dispose();
            _consumer.Dispose();
        }
    }

    public class KafkaMessageConsumedEventArgs
    {
        public string Topic { get; }
        public string Message { get; }

        public KafkaMessageConsumedEventArgs(string topic, string message)
        {
            Topic = topic;
            Message = message;
        }
    }
}
