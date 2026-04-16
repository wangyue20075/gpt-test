using Oc.BinGrid.Core.Abstractions;
using System.Threading.Channels;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Messaging
{
    /// <summary>
    /// 高性能持久化通道实现
    /// </summary>
    public sealed class PersistenceChannel : IPersistenceChannel, ITransientDependency
    {
        private readonly Channel<object> _channel;

        public ChannelReader<object> Reader => _channel.Reader;

        public PersistenceChannel()
        {
            var options = new BoundedChannelOptions(50_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _channel = Channel.CreateBounded<object>(options);
        }

        public ValueTask EnqueueAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(entity, cancellationToken);
        }

        public async ValueTask EnqueueBatchAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await _channel.Writer.WriteAsync(entity, cancellationToken);
            }
        }

        public void Complete()
        {
            _channel.Writer.TryComplete();
        }
    }
}
