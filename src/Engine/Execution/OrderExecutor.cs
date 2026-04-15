using Oc.BinGrid.Domain.Aggregates.Strategy;
using Oc.BinGrid.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Engine.Executors
{
    public class OrderExecutor
    {
        private readonly IExchangeGateway _exchange;

        public OrderExecutor(IExchangeGateway exchange)
        {
            _exchange = exchange;
        }

        public async Task ExecuteAsync(StrategySignal signal)
        {
           
                
        }
    }
}
