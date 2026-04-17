using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 交易所网关接口：提供统一的底层通讯抽象，解耦具体交易所实现
    /// </summary>
    public interface IExchangeGateway
    {
        #region 1. 行情数据 (Market Data)

        /// <summary>
        /// 获取最新深度/价格 (用于冷启动或轮询)
        /// </summary>
        Task<TickData?> GetLatestTickAsync(string symbol);

        /// <summary>
        /// 获取历史 K 线
        /// </summary>
        Task<List<KlineData>> GetKlinesAsync(string symbol, string interval, int limit = 100);

        #endregion

        #region 2. 账户与资产 (Account & Asset)

        /// <summary>
        /// 获取可用余额
        /// </summary>
        Task<AssetBalance> GetBalanceAsync(string asset);

        /// <summary>
        /// 从交易所拉取所有当前挂单 (用于对账恢复)
        /// </summary>
        Task<List<OrderResponse>> GetOpenOrdersAsync(string symbol);

        #endregion

        #region 3. 交易执行 (Trade Execution)

        /// <summary>
        /// 下限价单
        /// </summary>
        /// <param name="symbol">交易对</param>
        /// <param name="side">BUY/SELL</param>
        /// <param name="price">价格</param>
        /// <param name="quantity">数量</param>
        /// <param name="clientOrderId">自定义ID，用于幂等和追踪</param>
        Task<OrderResponse?> PlaceLimitOrderAsync(string symbol, string side, decimal price, decimal quantity, string? clientOrderId = null);

        /// <summary>
        /// 下市价单
        /// </summary>
        Task<OrderResponse?> PlaceMarketOrderAsync(string symbol, string side, decimal quantity, string? clientOrderId = null);

        /// <summary>
        /// 查询订单详情
        /// </summary>
        Task<OrderResponse?> GetOrderAsync(string symbol, string orderId);

        /// <summary>
        /// 撤销订单
        /// </summary>
        Task<bool> CancelOrderAsync(string symbol, string orderId);

        #endregion

        #region 4. 推送订阅 (Streaming)

        /// <summary>
        /// 订阅行情流
        /// </summary>
        Task SubscribeSymbolTickerAsync(IEnumerable<string> symbols, Func<TickData, Task> onTick);

        /// <summary>
        /// 订阅私有数据流 (订单更新、余额变动)
        /// </summary>
        Task SubscribeUserDataAsync(Func<OrderResponse, Task> onOrderUpdate, Func<AssetBalance, Task> onBalanceUpdate);

        /// <summary>
        /// 停止所有订阅并关闭连接
        /// </summary>
        Task UnsubscribeAllAsync();

        #endregion
    }
}
