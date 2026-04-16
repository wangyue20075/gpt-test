using System.Threading.Channels;

namespace Oc.BinGrid.Core.Abstractions
{
    /// <summary>
    /// 持久化异步通道抽象
    /// 负责将领域对象推入异步写入队列
    /// </summary>
    public interface IPersistenceChannel
    {
        /// <summary>
        /// 写入一个持久化对象（非阻塞）
        /// </summary>
        ValueTask EnqueueAsync(object entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量写入
        /// </summary>
        ValueTask EnqueueBatchAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取端（供 PersistenceWorker 消费）
        /// </summary>
        ChannelReader<object> Reader { get; }

        /// <summary>
        /// 标记完成
        /// </summary>
        void Complete();
    }
}
