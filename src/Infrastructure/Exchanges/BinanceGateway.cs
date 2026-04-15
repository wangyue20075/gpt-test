using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using System.Text.Json.Serialization;

namespace Oc.BinGrid.Infrastructure.Exchanges
{
    public class BinanceGateway : IExchangeGateway
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogger<BinanceGateway> _logger;

        public BinanceGateway(IConfiguration config, ILogger<BinanceGateway> logger)
        {
            _baseUrl = config["Binance:BaseUrl"] ?? "https://api.binance.com";
            _apiKey = config["Binance:ApiKey"];
            _apiSecret = config["Binance:ApiSecret"];
            _logger = logger;
        }

        #region 公共接口 (Public API)

        public async Task<TickData> GetLatestTickAsync(string symbol)
        {
            var result = await _baseUrl
                .AppendPathSegment("/api/v3/ticker/price")
                .SetQueryParam("symbol", symbol.ToUpper())
                .GetJsonAsync<TickerPriceResponse>();

            return new TickData(symbol, result.Price, 0, DateTime.Now);
        }

        public async Task<List<KlineData>> GetKlinesAsync(string symbol, string interval, int limit = 100)
        {
            var result = await _baseUrl
                .AppendPathSegment("/api/v3/klines")
                .SetQueryParam("symbol", symbol.ToUpper())
                .SetQueryParam("interval", interval)
                .SetQueryParam("limit", limit)
                .GetJsonAsync<List<List<object>>>();

            return result.Select(x => new KlineData(
                OpenTime: DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(x[0])).DateTime,
                Open: decimal.Parse(x[1].ToString()),
                High: decimal.Parse(x[2].ToString()),
                Low: decimal.Parse(x[3].ToString()),
                Close: decimal.Parse(x[4].ToString()),
                Volume: decimal.Parse(x[5].ToString())
            )).ToList();
        }

        #endregion

        #region 私有接口 (Private API - Requires Signature)

        public async Task<AssetBalance> GetBalanceAsync(string asset)
        {
            var response = await SendSignedRequestAsync("/api/v3/account", HttpMethod.Get);
            var accountInfo = await response.GetJsonAsync<AccountInfoResponse>();

            var balance = accountInfo.Balances.FirstOrDefault(b => b.Asset == asset.ToUpper());
            return new AssetBalance
            {
                Asset = asset,
                Free = balance?.Free ?? 0,
                Locked = balance?.Locked ?? 0
            };
        }

        public async Task<TradeOrder> PlaceLimitOrderAsync(string symbol, string side, decimal price, decimal quantity)
        {
            var args = new Dictionary<string, object>
        {
            { "symbol", symbol.ToUpper() },
            { "side", side.ToUpper() },
            { "type", "LIMIT" },
            { "timeInForce", "GTC" },
            { "quantity", quantity.ToString("F8") }, // 强制保留精度防止科学计数法
            { "price", price.ToString("F8") }
        };

            var response = await SendSignedRequestAsync("/api/v3/order", HttpMethod.Post, args);
            var order = await response.GetJsonAsync<OrderResponse>();

            return new TradeOrder
            {
                OrderId = order.OrderId.ToString(),
                Symbol = symbol,
                Price = order.Price,
                Quantity = order.OrigQty,
                Side = side,
                Status = order.Status,
                CreateTime = DateTime.Now
            };
        }

        public async Task<bool> CancelOrderAsync(string symbol, string orderId)
        {
            var args = new Dictionary<string, object> {
            { "symbol", symbol.ToUpper() },
            { "orderId", orderId }
        };
            await SendSignedRequestAsync("/api/v3/order", HttpMethod.Delete, args);
            return true;
        }

        #endregion

        #region 核心工具方法

        private async Task<IFlurlResponse> SendSignedRequestAsync(string path, HttpMethod method, Dictionary<string, object>? queryParams = null)
        {
            queryParams ??= new Dictionary<string, object>();
            queryParams["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            queryParams["recvWindow"] = 5000;

            // 1. 拼接 QueryString 用于签名
            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var signature = BinanceSigner.CreateSignature(queryString, _apiSecret);

            // 2. 发送请求
            var request = _baseUrl
                .AppendPathSegment(path)
                .WithHeader("X-MBX-APIKEY", _apiKey)
                .SetQueryParams(queryParams)
                .SetQueryParam("signature", signature);

            try
            {
                return method switch
                {
                    var m when m == HttpMethod.Get => await request.GetAsync(),
                    var m when m == HttpMethod.Post => await request.PostAsync(),
                    var m when m == HttpMethod.Delete => await request.DeleteAsync(),
                    _ => throw new NotSupportedException()
                };
            }
            catch (FlurlHttpException ex)
            {
                var errorBody = await ex.GetResponseStringAsync();
                _logger.LogError("币安 API 报错: {Status} | {Body}", ex.StatusCode, errorBody);
                throw;
            }
        }

        // 实时监听暂不实现，通常由独立的 WebSocket 模块处理
        public void SubscribeTickStream(string symbol, Action<TickData> onTick) => throw new NotImplementedException("请使用专门的 WebSocketWorker");
        public void SubscribeUserDataStream(Action<TradeOrder> onOrderUpdate, Action<AssetBalance> onBalanceUpdate) => throw new NotImplementedException();
        public Task<List<Position>> GetPositionsAsync(string symbol = null) => throw new NotImplementedException();
        public Task<TradeOrder> PlaceMarketOrderAsync(string symbol, string side, decimal quantity) => throw null!;
        public Task<TradeOrder> GetOrderAsync(string symbol, string orderId) => throw null!;

        #endregion

        #region DTOs (Data Transfer Objects)

        

        /// <summary>
        /// 币安价格响应 DTO
        /// </summary>
        private record TickerPriceResponse(
            [property: JsonPropertyName("symbol")] string Symbol,
            // 关键： property 确保特性应用在属性上，AllowReadingFromString 解决字符串转 decimal 问题
            [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            [property: JsonPropertyName("price")] decimal Price
        );

        private record AccountInfoResponse(List<BalanceDto> Balances);
        private record BalanceDto(string Asset, decimal Free, decimal Locked);


        private record OrderResponse(
            [property: JsonPropertyName("orderId")] long OrderId,
            [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            [property: JsonPropertyName("price")] decimal Price,
            [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            [property: JsonPropertyName("origQty")] decimal OrigQty,
            [property: JsonPropertyName("status")] string Status
        );

        #endregion
    }
}
