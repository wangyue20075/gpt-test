using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Strategies.Grid
{
    public class GridManager : ISingletonDependency
    {
        private readonly GridConfig _config;
        private readonly IMarketGateway _gateway;
        private readonly IPositionRepository _posRepo;
        private readonly IOrderRepository _orderRepo;

        public GridManager(
            GridConfig config,
            IMarketGateway gateway,
            IPositionRepository posRepo,
            IOrderRepository orderRepo)
        {
            _config = config;
            _gateway = gateway;
            _posRepo = posRepo;
            _orderRepo = orderRepo;
        }

        /// <summary>
        /// 核心逻辑：处理每一个价格变动
        /// </summary>
        public async Task ExecuteTickAsync(decimal currentPrice)
        {
            // 1. 更新价格追踪极值
            _config.TrackedHigh = Math.Max(_config.TrackedHigh, currentPrice);
            _config.TrackedLow = _config.TrackedLow == 0 ? currentPrice : Math.Min(_config.TrackedLow, currentPrice);

            // 2. 检查下跌反弹 (买入/补仓逻辑)
            if (currentPrice >= _config.BasePrice * (1 - _config.DownThreshold))
            {
                // 检查是否从低点反弹了指定的比例
                decimal reboundPrice = _config.TrackedLow * (1 + (_config.DownRebound ?? 0));
                if (currentPrice >= reboundPrice && _config.TrackedLow < _config.BasePrice)
                {
                    await FireOrderAsync(OrderAction.OpenLong, currentPrice);
                }
            }

            // 3. 检查上涨回落 (卖出/减仓逻辑)
            if (currentPrice <= _config.BasePrice * (1 + _config.UpThreshold))
            {
                decimal retracementPrice = _config.TrackedHigh * (1 - (_config.UpRetracement ?? 0));
                if (currentPrice <= retracementPrice && _config.TrackedHigh > _config.BasePrice)
                {
                    await FireOrderAsync(OrderAction.CloseLong, currentPrice);
                }
            }
        }

        private async Task FireOrderAsync(OrderAction action, decimal price)
        {
            // 创建订单实体
            var order = new EaOrder(_config.Name, _config.Symbol, _config.Market, action, price, _config.TradeAmount, OrderType.Market);

            // 发送订单
            var resp = await _gateway.PlaceOrderAsync(order);
            if (resp.Success)
            {
                order.MarkSubmitted(resp.ExchangeOrderId ?? 0);

                // 更新网格状态并入库
                _config.UpdateStateAfterTrade(price);
                await _orderRepo.SaveAsync(order);
            }
        }
    }
}
