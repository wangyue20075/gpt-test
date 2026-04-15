using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        public Task<List<TradeOrder>> GetRecentOrdersAsync(int count)
        {
            throw new NotImplementedException();
        }

        public Task InsertAsync(TradeOrder order)
        {
            throw new NotImplementedException();
        }
    }
}
