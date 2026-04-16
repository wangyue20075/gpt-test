using Microsoft.Extensions.Logging;
using Oc.BinGrid.Core.Abstractions;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using Oc.BinGrid.Engine.Interfaces;

namespace Oc.BinGrid.Engine.Strategies.Grid
{
    public class GridStrategy : StrategyBase
    {
        private readonly GridSetting _setting;
        private readonly IOrderMonitorService _monitor;

        // 策略核心状态
        private decimal _currentBasePrice;                   // 当前动态基准价
        private readonly List<decimal> _openedPrices = new(); // 已开仓的价格栈 (LIFO)

        // 观察窗口状态
        private decimal? _observedBottom; // 下跌观察最低点
        private decimal? _observedHigh;   // 盈利观察最高点

        public override string Symbol => _setting.Symbol;

        public GridStrategy(
            ILogger<GridStrategy> logger,
            GridSetting setting,
            IExchangeGateway gateway,
            IOrderRepository orderRepo,
            IPersistenceChannel persistenceChannel,
            IOrderMonitorService monitor
            )
            : base(logger, gateway, orderRepo, persistenceChannel)
        {
            _setting = setting;
            _monitor = monitor;
            _currentBasePrice = setting.InitialPrice;
            Name = $"Grid_{setting.Symbol}_{setting.GridGap}";
        }

        protected override async Task HandleTickAsync(TickData tick)
        {
            // 如果当前有正在处理的挂单，则静默等待，不进入新逻辑
            if (GetActiveOrders().Any()) return;

            // 1. 优先处理平仓逻辑（止盈）
            await HandleExitLogic(tick);

            // 2. 处理开仓逻辑（只有平仓没发单的情况下才查开仓）
            if (!GetActiveOrders().Any())
            {
                await HandleEntryLogic(tick);
            }
        }

        /// <summary>
        /// 核心逻辑：当订单状态改变（成交/撤销）时，由基类回调此方法
        /// </summary>
        protected override async Task HandleOrderUpdateAsync(OrderResponse order)
        {
            if (order.Status == "FILLED")
            {
                if (order.Side == "BUY")
                {
                    _openedPrices.Add(order.Price);
                    _currentBasePrice = order.Price;
                    Logger.LogInformation("✅ 买单成交，新基准价: {Price}，总持仓: {Count}", order.Price, _openedPrices.Count);
                }
                else // SELL
                {
                    if (_openedPrices.Any()) _openedPrices.RemoveAt(_openedPrices.Count - 1);
                    UpdateBasePriceAfterExit(order.Price);
                    Logger.LogWarning("💰 卖单成交，平仓价格: {Price}，剩余持仓: {Count}", order.Price, _openedPrices.Count);
                }
            }
            else if (order.Status == "CANCELED")
            {
                Logger.LogInformation("ℹ️ 挂单已由监控服务撤销 (超时或价格偏离)，策略已解锁。");
            }

            // 重置观察窗，等待下一轮 Tick 重新触发
            _observedBottom = null;
            _observedHigh = null;
        }

        #region 开仓逻辑 - 动态反弹买入

        private async Task HandleEntryLogic(TickData tick)
        {
            if (_openedPrices.Count >= _setting.MaxGrids) return;

            decimal buyThreshold = _currentBasePrice - _setting.GridGap;

            // 触发条件：跌破网格线
            if (!_observedBottom.HasValue && tick.Price <= buyThreshold)
            {
                _observedBottom = tick.Price;
                Logger.LogDebug("📉 触及买入线 {Level}，开启反弹观察...", buyThreshold);
            }

            if (_observedBottom.HasValue)
            {
                _observedBottom = Math.Min(_observedBottom.Value, tick.Price);

                // 反弹买入条件
                if (tick.Price >= _observedBottom.Value * (1 + _setting.ReboundRate))
                {
                    await PlaceGridOrderAsync("BUY", tick.Price);
                }
            }
        }

        #endregion

        #region 平仓逻辑 - 追踪止盈回撤

        private async Task HandleExitLogic(TickData tick)
        {
            if (_openedPrices.Count == 0) return;

            decimal lastEntryPrice = _openedPrices.Last();
            decimal currentProfit = (tick.Price - lastEntryPrice) / lastEntryPrice;

            // 达到止盈门槛
            if (!_observedHigh.HasValue && currentProfit >= _setting.TakeProfit)
            {
                _observedHigh = tick.Price;
                Logger.LogInformation("🎯 盈利达标 {Profit:P2}，启动追踪止盈...", currentProfit);
            }

            if (_observedHigh.HasValue)
            {
                _observedHigh = Math.Max(_observedHigh.Value, tick.Price);
                decimal callback = (_observedHigh.Value - tick.Price) / _observedHigh.Value;

                // 回撤平仓条件
                if (callback >= _setting.CallbackRate)
                {
                    await PlaceGridOrderAsync("SELL", tick.Price);
                }
            }
        }

        #endregion

        #region 订单与监控集成

        private async Task PlaceGridOrderAsync(string side, decimal price)
        {
            if (ActiveOrders.Any()) return;

            Logger.LogInformation("🚀 发送 {Side} 限价单: {Price} | Symbol: {Symbol}", side, price, Symbol);

            var order = await Gateway.PlaceLimitOrderAsync(Symbol, side, price, _setting.QuantityPerGrid);

            if (order != null)
            {
                var orderEntity = MapToEntity(order);

                // 1. 设置基类锁，防止重入
                ActiveOrders.TryAdd(order.OrderId, orderEntity);   

                // 2. 注册到监控服务 (超时或价格跑太远则撤单)
                _monitor.Watch(new OrderWatchTask
                {
                    OrderId = order.OrderId,
                    Symbol = Symbol,
                    OrderPrice = price,
                    CreateTime = DateTime.UtcNow,
                    OwnerStrategy = this,
                    Timeout = TimeSpan.FromSeconds(60), // 可从 Setting 读取
                    MaxDeviation = 0.01m                // 偏离 1% 撤单
                });
            }
        }

        private void UpdateBasePriceAfterExit(decimal exitPrice)
        {
            // 如果还有剩余仓位，基准价回退到剩下仓位中最深的一个
            // 如果仓位清空，将当前成交价作为新起点（或 InitialPrice）
            _currentBasePrice = _openedPrices.Any() ? _openedPrices.Last() : exitPrice;
            Logger.LogInformation("🔄 基准价调整为: {Base}", _currentBasePrice);
        }

        #endregion
    }
}
