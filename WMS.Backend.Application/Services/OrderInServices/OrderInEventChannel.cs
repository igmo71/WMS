using System.Threading.Channels;
using WMS.Backend.Common;

namespace WMS.Backend.Application.Services.OrderInServices
{
    internal class OrderInEventChannel
    {
        private readonly Channel<IAppEvent> _channel;

        public OrderInEventChannel()
        {
            _channel = Channel.CreateUnbounded<IAppEvent>();
        }

        public ChannelWriter<IAppEvent> Writer => _channel.Writer;

        public ChannelReader<IAppEvent> Reader => _channel.Reader;
    }
}
