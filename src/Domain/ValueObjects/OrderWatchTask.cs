using Oc.BinGrid.Domain.Interfaces;

namespace Oc.BinGrid.Domain.ValueObjects
{
    // 监控任务的数据载体
    public class OrderWatchTask
    {
        public string OrderId { get; init; }
        public string Symbol { get; init; }
        public decimal OrderPrice { get; init; }
        public DateTime CreateTime { get; init; }

        // 引用 Domain 层的策略接口，避免循环引用
        public IStrategy OwnerStrategy { get; init; }

        // 撤单触发参数
        public TimeSpan Timeout { get; init; }
        public decimal MaxDeviation { get; init; }
    }
}
