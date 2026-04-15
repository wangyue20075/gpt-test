using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 交易所网关接口：屏蔽不同交易所 API 差异
    /// </summary>
    public interface IExchangeGateway
    {
        #region 1. 行情数据 (Market Data)

        /// <summary>
        /// 获取单个交易对的最新价格 (轮询模式使用)
        /// </summary>
        Task<TickData> GetLatestTickAsync(string symbol);

        /// <summary>
        /// 获取 K 线数据 (用于计算技术指标如 MA, RSI)
        /// </summary>
        Task<List<KlineData>> GetKlinesAsync(string symbol, string interval, int limit = 100);

        #endregion

        #region 2. 资产与仓位 (Account & Position)

        /// <summary>
        /// 获取账户余额 (如 USDT)
        /// </summary>
        Task<AssetBalance> GetBalanceAsync(string asset);

        /// <summary>
        /// 获取当前活跃仓位 (多仓或空仓)
        /// </summary>
        Task<List<Position>> GetPositionsAsync(string symbol = null);

        #endregion

        #region 3. 交易执行 (Trade Execution)

        /// <summary>
        /// 下限价单 (网格策略常用)
        /// </summary>
        /// <param name="symbol">交易对</param>
        /// <param name="side">BUY / SELL</param>
        /// <param name="price">价格</param>
        /// <param name="quantity">数量</param>
        /// <returns>返回标准化的订单实体</returns>
        Task<TradeOrder> PlaceLimitOrderAsync(string symbol, string side, decimal price, decimal quantity);

        /// <summary>
        /// 下市价单 (止损或紧急平仓常用)
        /// </summary>
        Task<TradeOrder> PlaceMarketOrderAsync(string symbol, string side, decimal quantity);

        /// <summary>
        /// 撤销单个订单
        /// </summary>
        Task<bool> CancelOrderAsync(string symbol, string orderId);

        /// <summary>
        /// 获取订单当前状态
        /// </summary>
        Task<TradeOrder> GetOrderAsync(string symbol, string orderId);

        #endregion

        #region 4. 实时监听 (Stream / WebSocket)

        /// <summary>
        /// 开启 WebSocket 价格推送 (由外部 Worker 调用)
        /// </summary>
        void SubscribeTickStream(string symbol, Action<TickData> onTick);

        /// <summary>
        /// 开启 订单状态/余额变动 推送
        /// </summary>
        void SubscribeUserDataStream(Action<TradeOrder> onOrderUpdate, Action<AssetBalance> onBalanceUpdate);

        #endregion
    }
}
