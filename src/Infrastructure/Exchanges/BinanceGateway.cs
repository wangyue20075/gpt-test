using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;

namespace Oc.BinGrid.Infrastructure.Exchanges
{
    public class BinanceGateway : IMarketGateway
    {
        private const string SpotProdBaseUrl = "https://api.binance.com";
        private const string SpotTestBaseUrl = "https://testnet.binance.vision";
        private const string FuturesProdBaseUrl = "https://fapi.binance.com";
        private const string FuturesTestBaseUrl = "https://testnet.binancefuture.com";

        private readonly ILogger<BinanceGateway> _logger;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly bool _useTestnet;

        public BinanceGateway(IConfiguration configuration, ILogger<BinanceGateway> logger)
        {
            _logger = logger;
            _apiKey = configuration["Binance:ApiKey"] ?? string.Empty;
            _apiSecret = configuration["Binance:ApiSecret"] ?? string.Empty;
            _useTestnet = bool.TryParse(configuration["Binance:UseTestnet"], out var useTest) && useTest;
        }

        public async Task<object> PlaceOrderAsync(EaOrder order)
        {
            EnsureCredential();

            var endpoint = order.Market == MarketType.Futures ? "/fapi/v1/order" : "/api/v3/order";
            var baseUrl = ResolveBaseUrl(order.Market);

            var parameters = new Dictionary<string, string>
            {
                ["symbol"] = order.Symbol,
                ["side"] = order.Side == OrderSide.Buy ? "BUY" : "SELL",
                ["type"] = order.OrderType == Domain.Enums.OrderType.Market ? "MARKET" : "LIMIT",
                ["quantity"] = order.OrderQty.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["newOrderRespType"] = "RESULT"
            };

            if (order.OrderType == Domain.Enums.OrderType.Limit)
            {
                parameters["price"] = order.OrderPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
                parameters["timeInForce"] = "GTC";
            }

            if (order.Market == MarketType.Futures)
            {
                parameters["reduceOnly"] = order.IsReduceOnly.ToString().ToLowerInvariant();
            }

            var response = await SendSignedAsync(baseUrl, endpoint, parameters, HttpMethod.Post);

            var exchangeOrderId = response.RootElement.TryGetProperty("orderId", out var orderIdProp)
                ? orderIdProp.GetInt64()
                : 0;

            _logger.LogInformation(
                "[Binance] PlaceOrder success Symbol={Symbol} Side={Side} Qty={Qty} ExchangeOrderId={ExchangeOrderId}",
                order.Symbol,
                order.Side,
                order.OrderQty,
                exchangeOrderId);

            return new
            {
                Success = true,
                ExchangeOrderId = exchangeOrderId,
                Raw = response.RootElement.GetRawText()
            };
        }

        public Task<object> CancelOrderAsync(long exchangeOrderId)
        {
            EnsureCredential();
            throw new InvalidOperationException($"CancelOrderAsync requires symbol context for Binance API. exchangeOrderId={exchangeOrderId}");
        }

        public async Task<decimal> GetLatestPriceAsync(string symbol)
        {
            var baseUrl = _useTestnet ? SpotTestBaseUrl : SpotProdBaseUrl;
            var json = await $"{baseUrl}/api/v3/ticker/price"
                .SetQueryParam("symbol", symbol)
                .GetJsonAsync<JsonElement>();

            if (!json.TryGetProperty("price", out var priceProperty))
            {
                throw new InvalidOperationException($"Invalid ticker response: {json.GetRawText()}");
            }

            if (!decimal.TryParse(priceProperty.GetString(), out var price))
            {
                throw new InvalidOperationException($"Unable to parse price from response: {json.GetRawText()}");
            }

            return price;
        }

        private async Task<JsonDocument> SendSignedAsync(
            string baseUrl,
            string endpoint,
            Dictionary<string, string> parameters,
            HttpMethod method)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            parameters["timestamp"] = timestamp;

            var queryString = string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var signature = Sign(queryString, _apiSecret);
            var signedQuery = $"{queryString}&signature={signature}";

            var request = $"{baseUrl}{endpoint}".WithHeader("X-MBX-APIKEY", _apiKey);
            string content;

            if (method == HttpMethod.Delete)
            {
                content = await $"{baseUrl}{endpoint}?{signedQuery}"
                    .WithHeader("X-MBX-APIKEY", _apiKey)
                    .DeleteAsync()
                    .ReceiveString();
            }
            else
            {
                content = await request
                    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                    .PostStringAsync(signedQuery)
                    .ReceiveString();
            }

            return JsonDocument.Parse(content);
        }

        private string ResolveBaseUrl(MarketType market)
        {
            if (market == MarketType.Futures)
            {
                return _useTestnet ? FuturesTestBaseUrl : FuturesProdBaseUrl;
            }

            return _useTestnet ? SpotTestBaseUrl : SpotProdBaseUrl;
        }

        private static string Sign(string payload, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private void EnsureCredential()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret))
            {
                throw new InvalidOperationException("Binance API credentials are not configured. Please set Binance:ApiKey and Binance:ApiSecret.");
            }
        }
    }
}
