using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using System.Text.Json.Serialization;

namespace Oc.BinGrid.Infrastructure.Exchanges;

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

    #region 1. 行情与公共数据

    public async Task<TickData> GetLatestTickAsync(string symbol)
    {
        var result = await _baseUrl
            .AppendPathSegment("/api/v3/ticker/price")
            .SetQueryParam("symbol", symbol.ToUpper())
            .GetJsonAsync<TickerPriceResponse>();

        return new TickData(symbol, result.Price, 0, DateTime.UtcNow);
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
            Open: decimal.Parse(x[1].ToString()!),
            High: decimal.Parse(x[2].ToString()!),
            Low: decimal.Parse(x[3].ToString()!),
            Close: decimal.Parse(x[4].ToString()!),
            Volume: decimal.Parse(x[5].ToString()!)
        )).ToList();
    }

    #endregion

    #region 2. 交易执行 (核心重构：返回 OrderResponse)

    public async Task<OrderResponse?> PlaceLimitOrderAsync(string symbol, string side, decimal price, decimal quantity, string? clientOrderId = null)
    {
        var args = new Dictionary<string, object>
        {
            { "symbol", symbol.ToUpper() },
            { "side", side.ToUpper() },
            { "type", "LIMIT" },
            { "timeInForce", "GTC" },
            { "quantity", quantity.ToString("F8") },
            { "price", price.ToString("F8") }
        };

        if (!string.IsNullOrEmpty(clientOrderId)) args["newClientOrderId"] = clientOrderId;

        try
        {
            var response = await SendSignedRequestAsync("/api/v3/order", HttpMethod.Post, args);
            var binanceOrder = await response.GetJsonAsync<BinanceOrderResponse>();
            return MapToDomainOrder(binanceOrder, symbol);
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 400)
        {
            _logger.LogError("下单被拒 (400): {Body}", await ex.GetResponseStringAsync());
            return null;
        }
    }

    public async Task<OrderResponse?> GetOrderAsync(string symbol, string orderId)
    {
        var args = new Dictionary<string, object> { { "symbol", symbol.ToUpper() }, { "orderId", orderId } };
        var response = await SendSignedRequestAsync("/api/v3/order", HttpMethod.Get, args);
        var binanceOrder = await response.GetJsonAsync<BinanceOrderResponse>();
        return MapToDomainOrder(binanceOrder, symbol);
    }

    public async Task<bool> CancelOrderAsync(string symbol, string orderId)
    {
        try
        {
            var args = new Dictionary<string, object> { { "symbol", symbol.ToUpper() }, { "orderId", orderId } };
            await SendSignedRequestAsync("/api/v3/order", HttpMethod.Delete, args);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤单失败: {Id}", orderId);
            return false;
        }
    }

    #endregion

    #region 3. 账户与对账

    public async Task<AssetBalance> GetBalanceAsync(string asset)
    {
        var response = await SendSignedRequestAsync("/api/v3/account", HttpMethod.Get);
        var accountInfo = await response.GetJsonAsync<BinanceAccountInfo>();

        var balance = accountInfo.Balances.FirstOrDefault(b => b.Asset.Equals(asset, StringComparison.OrdinalIgnoreCase));
        return new AssetBalance { Asset = asset, Free = balance?.Free ?? 0, Locked = balance?.Locked ?? 0 };
    }

    public async Task<List<OrderResponse>> GetOpenOrdersAsync(string symbol)
    {
        var args = new Dictionary<string, object> { { "symbol", symbol.ToUpper() } };
        var response = await SendSignedRequestAsync("/api/v3/openOrders", HttpMethod.Get, args);
        var binanceOrders = await response.GetJsonAsync<List<BinanceOrderResponse>>();

        return binanceOrders.Select(o => MapToDomainOrder(o, symbol)).ToList();
    }

    #endregion

    #region 4. WebSocket (占位契约)

    public Task SubscribeSymbolTickerAsync(IEnumerable<string> symbols, Func<TickData, Task> onTick) => Task.CompletedTask;
    public Task SubscribeUserDataAsync(Func<OrderResponse, Task> onOrderUpdate, Func<AssetBalance, Task> onBalanceUpdate) => Task.CompletedTask;
    public Task UnsubscribeAllAsync() => Task.CompletedTask;
    public Task<OrderResponse?> PlaceMarketOrderAsync(string symbol, string side, decimal quantity, string? clientOrderId = null) => throw new NotImplementedException();

    #endregion

    #region 私有辅助方法

    private async Task<IFlurlResponse> SendSignedRequestAsync(string path, HttpMethod method, Dictionary<string, object>? queryParams = null)
    {
        queryParams ??= new Dictionary<string, object>();
        queryParams["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        queryParams["recvWindow"] = 5000;

        string queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
        string signature = BinanceSigner.CreateSignature(queryString, _apiSecret);

        var request = _baseUrl.AppendPathSegment(path)
            .WithHeader("X-MBX-APIKEY", _apiKey)
            .SetQueryParams(queryParams)
            .SetQueryParam("signature", signature);

        return method switch
        {
            _ when method == HttpMethod.Get => await request.GetAsync(),
            _ when method == HttpMethod.Post => await request.PostAsync(),
            _ when method == HttpMethod.Delete => await request.DeleteAsync(),
            _ => throw new NotSupportedException()
        };
    }

    private OrderResponse MapToDomainOrder(BinanceOrderResponse o, string symbol)
    {
        return new OrderResponse(
            OrderId: o.OrderId.ToString(),
            Symbol: symbol,
            Status: o.Status,
            Side: o.Side,
            Price: o.Price,
            Quantity: o.OrigQty,
            ExecutedQty: o.ExecutedQty,
            ExecutedPrice: o.ExecutedQty > 0 ? (o.CummulativeQuoteQty / o.ExecutedQty) : 0, // 💡 重点：计算真实均价
            UpdateTime: DateTimeOffset.FromUnixTimeMilliseconds(o.UpdateTime > 0 ? o.UpdateTime : o.TransactTime).DateTime,
            ClientOrderId: o.ClientOrderId
        );
    }

    #endregion

    #region DTOs (Data Transfer Objects)

    private record TickerPriceResponse(string Symbol, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Price);
    private record BinanceAccountInfo(List<BinanceBalanceDto> Balances);
    private record BinanceBalanceDto(string Asset, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Free, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Locked);

    private record BinanceOrderResponse(
        long OrderId,
        string ClientOrderId,
        string Status,
        string Side,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Price,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal OrigQty,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal ExecutedQty,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal CummulativeQuoteQty,
        long UpdateTime,
        long TransactTime
    );

    #endregion
}