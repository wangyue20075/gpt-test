using Oc.BinGrid.Domain.Interfaces;

namespace Oc.BinGrid.Engine.Executors
{
    public class OrderExecutor
    {
        private readonly IExchangeGateway _exchange;

        public OrderExecutor(IExchangeGateway exchange)
        {
            _exchange = exchange;
        }

        public async Task ExecuteAsync()
        {
           
                
        }
    }
}
