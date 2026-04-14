using System.Threading.Channels;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Services
{
    public class PersistenceChannel : ISingletonDependency
    {
        private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();

        public ChannelWriter<object> Writer => _channel.Writer;
        public ChannelReader<object> Reader => _channel.Reader;

        public ValueTask EnqueueAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(entity, cancellationToken);
        }
    }
}
