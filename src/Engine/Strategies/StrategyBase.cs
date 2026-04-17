using Microsoft.Extensions.Logging;
using Oc.BinGrid.Core.Abstractions;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Strategies
{
    /// <summary>
    /// 策略基类（线程安全 + 生命周期可控 + 多订单支持）
    /// </summary>
    public abstract class StrategyBase : IStrategy, ISingletonDependency
    {
        protected readonly ILogger Logger;
        protected readonly IExchangeGateway Gateway;
        protected readonly IOrderRepository OrderRepo;
        protected readonly IPositionRepository PositionRepo;
        protected readonly IPersistenceChannel PersistenceChannel;

        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private int _exceptionCount;
        private const int MaxExceptionTolerance = 10;

        private StrategyState _state = StrategyState.Initializing;

        protected readonly ConcurrentDictionary<string, TradeOrder> ActiveOrders = new();
        protected readonly ConcurrentDictionary<string, GridPosition> ActivePositions = new();
        public decimal LastTickPrice { get; protected set; }

        public string Id { get; protected init; }
        public string Name { get; protected init; }
        public abstract string Symbol { get; }

        public StrategyState State => _state;
        public long TotalTicksProcessed { get; set; }
        public DateTime? LastTickTime { get; private set; }

        protected StrategyBase(
            ILogger logger,
            IExchangeGateway gateway,
            IOrderRepository orderRepo,
            IPositionRepository positionRepo,
            IPersistenceChannel persistenceChannel
            )
        {
            Logger = logger;
            Gateway = gateway;
            OrderRepo = orderRepo;
            PositionRepo = positionRepo;
            PersistenceChannel = persistenceChannel;
            Name = GetType().Name;
        }

        #region 生命周期管理

        public async Task StartAsync()
        {
            await _lifecycleLock.WaitAsync();
            try
            {
                if (_state == StrategyState.Running)
                    return;

                _state = StrategyState.Initializing;
                Logger.LogInformation("策略 {Name} ({Id}) 启动中...", Name, Id);

                // 1. 恢复：从 DB 找回挂单和持仓
                await RestoreAsync();
                // 2. 钩子：执行子类特定的启动逻辑
                await OnStartedAsync();

                _state = StrategyState.Running;
                Logger.LogInformation("策略 {Name} 启动成功。", Name);
            }
            catch (Exception ex)
            {
                _state = StrategyState.Faulted;
                Logger.LogCritical(ex, "策略 {Name} 启动失败。", Name);
                throw;
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        public async Task StopAsync()
        {
            await _lifecycleLock.WaitAsync();
            try
            {
                if (_state != StrategyState.Running)
                    return;

                _state = StrategyState.Stopped;

                await OnStoppingAsync();

                Logger.LogWarning("策略 {Name} 已停止。", Name);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        #endregion

        #region 核心事件驱动

        /// <summary>
        /// 行情驱动
        /// </summary>
        public async Task OnTickAsync(TickData tick)
        {
            if (_state != StrategyState.Running || tick.Symbol != Symbol) return;

            try
            {
                LastTickPrice = tick.Price;
                LastTickTime = DateTime.UtcNow;
                TotalTicksProcessed++;

                await HandleTickAsync(tick);
            }
            catch (Exception ex)
            {
                HandleStrategyException(ex);
            }
        }

        /// <summary>
        /// 订单更新
        /// </summary>
        public async Task OnOrderUpdateAsync(OrderResponse response)
        {
            try
            {
                // 1. 转换为实体 (自动补全本地上下文)
                var orderEntity = MapToEntity(response);

                // 2. 更新内存字典
                if (IsFinalStatus(orderEntity.Status.ToString()))
                {
                    ActiveOrders.TryRemove(response.OrderId, out _);
                }
                else
                {
                    ActiveOrders.AddOrUpdate(response.OrderId, orderEntity, (_, _) => orderEntity);
                }

                // 3. 异步持久化
                await PersistenceChannel.EnqueueAsync(orderEntity);

                Logger.LogInformation("策略 {Name} 订单 [{Id}] 状态 -> {Status}", Name, response.OrderId, response.Status);

                // 4. 执行子类具体的逻辑
                await HandleOrderUpdateAsync(response);
            }
            catch (Exception ex)
            {
                HandleStrategyException(ex);
            }
        }

        #endregion

        #region 恢复

        public async Task RestoreAsync()
        {
            Logger.LogInformation("策略 {Name} 正在执行状态重建...", Name);

            // 1. 恢复挂单 (Active Orders) -> 解决“不重复下单”
            var openOrders = await OrderRepo.GetOpenOrdersAsync(Id);
            ActiveOrders.Clear();
            foreach (var order in openOrders)
            {
                ActiveOrders.TryAdd(order.ExchangeOrderId, order);
            }

            // 2. 恢复持仓 (Positions) -> 解决“盈利记忆”
            var openPositions = await PositionRepo.GetOpenPositionsAsync(Id);
            ActivePositions.Clear();
            foreach (var pos in openPositions)
            {
                ActivePositions.TryAdd(pos.Id, pos);
            }

            // 3. 触发子类扩展钩子 (将数据传递给具体策略，如网格策略的价格栈)
            await OnRestoredAsync(openOrders, openPositions);

            Logger.LogInformation("策略 {Name} 状态重建完成。挂单: {OCount}, 持仓: {PCount}",
                Name, ActiveOrders.Count, openPositions.Count());
        }

        #endregion

        #region 工具方法

        protected bool IsFinalStatus(string status)
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

        public IReadOnlyCollection<TradeOrder> GetActiveOrders()
            => ActiveOrders.Values.ToList();

        public IReadOnlyCollection<PositionSnapshot> GetActivePositionSnapshots()
        {
            if (ActivePositions.IsEmpty) return Array.Empty<PositionSnapshot>();

            return ActivePositions.Values.Select(pos =>
            {
                // 计算浮盈
                decimal pnl = (LastTickPrice - pos.EntryPrice) * pos.Qty;
                // 如果是空头 (Short)，公式反转
                if (pos.Side == "SELL") pnl = (pos.EntryPrice - LastTickPrice) * pos.Qty;

                decimal pnlRate = pos.EntryPrice > 0 ? pnl / (pos.EntryPrice * pos.Qty) : 0;

                return new PositionSnapshot
                {
                    PositionId = pos.Id,
                    StrategyId = this.Id,
                    Symbol = this.Symbol,
                    Side = pos.Side,
                    EntryPrice = pos.EntryPrice,
                    Quantity = pos.Qty,
                    CurrentPrice = LastTickPrice,
                    ProfitLoss = pnl,
                    ProfitLossRate = pnlRate,
                    OpenedTime = pos.CreateTime
                };
            }).ToList();
        }

        #endregion

        #region 子类扩展点

        /// <summary>
        /// 子类必须实现行情逻辑
        /// </summary>
        protected abstract Task HandleTickAsync(TickData tick);

        /// <summary>
        /// 子类实现订单更新后的逻辑
        /// </summary>
        protected abstract Task HandleOrderUpdateAsync(OrderResponse order);

        /// <summary>
        /// 启动完成后
        /// </summary>
        protected virtual Task OnStartedAsync() => Task.CompletedTask;

        /// <summary>
        /// 停止前
        /// </summary>
        protected virtual Task OnStoppingAsync() => Task.CompletedTask;

        /// <summary>
        /// 恢复后
        /// </summary>
        protected virtual Task OnRestoredAsync(IEnumerable<TradeOrder> restoredOrders, IEnumerable<GridPosition> restoredPositions)
            => Task.CompletedTask;

        #endregion

        #region 异常熔断机制

        private void HandleStrategyException(Exception ex)
        {
            _exceptionCount++;

            Logger.LogError(ex,
                "策略 {Name} 异常，第 {Count} 次。",
                Name,
                _exceptionCount);

            if (_exceptionCount >= MaxExceptionTolerance)
            {
                _state = StrategyState.Faulted;

                Logger.LogCritical(
                    "策略 {Name} 已进入熔断状态！",
                    Name);
            }
        }

        /// <summary>
        /// 将交易所瞬时响应映射为领域持久化实体
        /// </summary>
        protected virtual TradeOrder MapToEntity(OrderResponse response)
        {
            // 1. 获取内存中的上下文记录（保留下单时的 PositionId, StrategyId 等核心基因）
            ActiveOrders.TryGetValue(response.OrderId, out var existing);

            return new TradeOrder
            {
                // --- 标识与关联 ---
                Id = existing?.Id ?? Guid.NewGuid().ToString("N"),
                ExchangeOrderId = response.OrderId,
                ClientOrderId = response.ClientOrderId ?? existing?.ClientOrderId,
                PositionId = existing?.PositionId,
                StrategyId = this.Id,
                StrategyName = this.Name,

                // --- 核心定义 (若 Response 缺失则从现有记录回填) ---
                Symbol = !string.IsNullOrEmpty(response.Symbol) ? response.Symbol : (existing?.Symbol ?? this.Symbol),
                Side = !string.IsNullOrEmpty(response.Side) ? response.Side : (existing?.Side ?? "UNKNOWN"),
                Type = !string.IsNullOrEmpty(response.OrderType) ? response.OrderType : (existing?.Type ?? "LIMIT"),

                // --- 价格与数量控制 ---
                // 委托价和委托数量通常在下单后不再改变，应优先保留 existing 中的值
                Price = existing?.Price > 0 ? existing.Price : response.Price,
                Qty = existing?.Qty > 0 ? existing.Qty : response.Quantity,

                // --- 执行状态 (以 Response 的最新进度为准) ---
                ExecPrice = response.ExecutedPrice, // 交易所计算的累计均价
                ExecQty = response.ExecutedQty,     // 交易所计算的累计成交量
                Status = ParseOrderState(response.Status),

                // --- 财务元数据 ---
                Fee = response.Fee,
                //FeeAsset = response.FeeAsset,

                // --- 时间戳 ---
                // 保持 CreateTime 不变，记录 UpdateTime 为本地当前时间
                CreateTime = existing?.CreateTime ?? response.UpdateTime,
                UpdateTime = DateTime.UtcNow,

                // 记录交易所真实的成交/变动时间
                ExecTime = response.UpdateTime
            };
        }

        /// <summary>
        /// 状态转换适配器：将交易所字符串状态转换为系统枚举
        /// </summary>
        private OrderState ParseOrderState(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return OrderState.New;

            return status.ToUpper() switch
            {
                "NEW" => OrderState.New,
                "PARTIALLY_FILLED" => OrderState.PartiallyFilled,
                "FILLED" => OrderState.Filled,
                "CANCELED" => OrderState.Canceled,
                "REJECTED" => OrderState.Rejected,
                "EXPIRED" => OrderState.Expired,
                _ => OrderState.New // 默认或抛出异常
            };
        }

        #endregion
    }
}
