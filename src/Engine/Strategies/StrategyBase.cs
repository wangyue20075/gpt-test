using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Engine.Strategies
{
    public abstract class StrategyBase : IStrategy
    {
        protected readonly IExchangeGateway Gateway;
        protected readonly IOrderRepository OrderRepo;
        protected readonly ILogger Logger;

        // 策略基本属性
        public string Id { get; protected init; } = Guid.NewGuid().ToString("N");
        public string Name { get; protected init; }
        public abstract string Symbol { get; }
        public bool IsEnabled { get; set; } = false;

        // 运行状态监控
        public DateTime? LastTickTime { get; private set; }
        public long TotalTicksProcessed { get; private set; }

        public string ActiveOrderId { get; protected set; }

        protected StrategyBase(
            IExchangeGateway gateway,
            IOrderRepository orderRepo,
            ILogger logger)
        {
            Gateway = gateway;
            OrderRepo = orderRepo;
            Logger = logger;
            Name = GetType().Name;
        }

        #region 生命周期管理

        /// <summary>
        /// 策略启动前的初始化逻辑（如恢复挂单、计算指标初始值）
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            Logger.LogInformation("策略 {Name} ({Id}) 启动初始化...", Name, Id);
            // 子类可在此处加载缓存、初始化技术指标等
            IsEnabled = true;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 策略停止时的清理逻辑
        /// </summary>
        public virtual async Task StopAsync()
        {
            IsEnabled = false;
            Logger.LogWarning("策略 {Name} 已停止接收行情。", Name);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 实现恢复逻辑：系统重启后重新绑定挂单 ID
        /// </summary>
        public virtual void RestoreActiveOrder(string orderId)
        {
            // ActiveOrderId = orderId;
            Logger.LogInformation("策略 {Name} 恢复挂单绑定: {OrderId}", Name, orderId);
        }

        #endregion

        #region 核心事件驱动

        /// <summary>
        /// 核心勾子：处理 Tick 数据
        /// </summary>
        public async Task OnTickAsync(TickData tick)
        {
            if (!IsEnabled || tick.Symbol != Symbol) return;

            try
            {
                LastTickTime = DateTime.Now;
                TotalTicksProcessed++;

                await HandleTickAsync(tick);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "策略 {Name} 处理 Tick 异常: {Symbol} @ {Price}", Name, tick.Symbol, tick.Price);
                // 可以在此处实现风险控制：比如异常超过 N 次自动停机
            }
        }

        /// <summary>
        /// 统一订单更新处理
        /// </summary>
        public virtual async Task OnOrderUpdateAsync(OrderResponse updatedOrder)
        {
            // 1. 全局持久化更新（更新数据库中的订单状态）
            //await OrderRepo.UpdateStatusAsync(updatedOrder.OrderId, updatedOrder.Status, updatedOrder.ExecutedPrice);

            // 2. 状态锁释放：如果是本策略关注的订单，且已到达终态，解锁
            //if (ActiveOrderId.HasValue && updatedOrder.OrderId == ActiveOrderId.Value)
            //{
            //    if (IsFinalStatus(updatedOrder.Status))
            //    {
            //        Logger.LogDebug("策略 {Name} 活跃订单 {Id} 已结束状态: {Status}，解锁状态位。", Name, updatedOrder.OrderId, updatedOrder.Status);
            //        ActiveOrderId = null;
            //    }
            //}

            // 2. 全局日志
            Logger.LogInformation("策略 {Name} 收到订单更新: {Id} | 状态: {Status}", Name, updatedOrder.OrderId, updatedOrder.Status);

            // 3. 检查是否异常（如被拒绝）
            if (updatedOrder.Status == "REJECTED")
            {
                Logger.LogCritical("策略 {Name} 委托订单被拒绝: {Id}", Name, updatedOrder.OrderId);
            }

            // 4. 调用子类特定的逻辑处理
            await HandleOrderActionAsync(updatedOrder);
        }

        #endregion

        #region 子类扩展点

        /// <summary>
        /// 由子类实现的具体策略逻辑
        /// </summary>
        protected abstract Task HandleTickAsync(TickData tick);

        /// <summary>
        /// 子类必须实现或重写：针对订单状态改变后的具体策略行为
        /// </summary>
        protected abstract Task HandleOrderActionAsync(OrderResponse order);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 判断订单是否处于终态（不再会发生变化）
        /// </summary>
        protected virtual bool IsFinalStatus(string status)
        {
            return status switch
            {
                "FILLED" => true,
                "CANCELED" => true,
                "REJECTED" => true,
                "EXPIRED" => true,
                _ => false
            };
        }

        #endregion
    }
}
