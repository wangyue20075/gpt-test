using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;

namespace Oc.BinGrid.Infrastructure.Exchanges
{
    public class BinanceGateway : IMarketGateway
    {
        public Task<object> CancelOrderAsync(long exchangeOrderId)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetLatestPriceAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<object> PlaceOrderAsync(EaOrder order)
        {
            throw new NotImplementedException();
        }
    }
}
