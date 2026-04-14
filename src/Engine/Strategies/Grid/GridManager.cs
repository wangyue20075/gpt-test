using System.Text.Json;
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
        private readonly IOrderRepository _orderRepo;
        private readonly IGridConfigRepository _configRepo;

        public GridManager(
            GridConfig config,
            IMarketGateway gateway,
            IOrderRepository orderRepo,
            IGridConfigRepository configRepo)
        {
            _config = config;
            _gateway = gateway;
            _orderRepo = orderRepo;
            _configRepo = configRepo;
        }

        public async Task ExecuteTickAsync(decimal currentPrice)
        {
            _config.TrackedHigh = Math.Max(_config.TrackedHigh, currentPrice);
            _config.TrackedLow = _config.TrackedLow == 0 ? currentPrice : Math.Min(_config.TrackedLow, currentPrice);

            if (ShouldOpenLong(currentPrice))
            {
                await FireOrderAsync(OrderAction.OpenLong, currentPrice);
                return;
            }

            if (ShouldCloseLong(currentPrice))
            {
                await FireOrderAsync(OrderAction.CloseLong, currentPrice);
            }
        }

        private bool ShouldOpenLong(decimal currentPrice)
        {
            var reboundPrice = _config.TrackedLow * (1 + (_config.DownRebound ?? 0));
            return currentPrice <= _config.BasePrice * (1 - _config.DownThreshold)
                   && currentPrice >= reboundPrice
                   && _config.TrackedLow < _config.BasePrice;
        }

        private bool ShouldCloseLong(decimal currentPrice)
        {
            var retracementPrice = _config.TrackedHigh * (1 - (_config.UpRetracement ?? 0));
            return currentPrice >= _config.BasePrice * (1 + _config.UpThreshold)
                   && currentPrice <= retracementPrice
                   && _config.TrackedHigh > _config.BasePrice;
        }

        private async Task FireOrderAsync(OrderAction action, decimal price)
        {
            var order = new EaOrder(_config.Name, _config.Symbol, _config.Market, action, price, _config.TradeAmount, OrderType.Market);

            var resp = await _gateway.PlaceOrderAsync(order);
            var exchangeOrderId = TryGetExchangeOrderId(resp);
            if (exchangeOrderId <= 0)
            {
                return;
            }

            order.MarkSubmitted(exchangeOrderId);
            await _orderRepo.SaveAsync(order);

            _config.UpdateStateAfterTrade(price);
            await _configRepo.SaveConfigAsync(_config);
        }

        private static long TryGetExchangeOrderId(object response)
        {
            try
            {
                var json = JsonSerializer.Serialize(response);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ExchangeOrderId", out var exchangeIdProp))
                {
                    return exchangeIdProp.GetInt64();
                }
            }
            catch
            {
                // ignored
            }

            return 0;
        }
    }
}
