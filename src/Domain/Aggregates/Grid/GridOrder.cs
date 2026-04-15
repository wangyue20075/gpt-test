using Oc.BinGrid.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Aggregates.Grid
{
    public class GridOrder
    {
        public Guid Id { get; private set; }
        public string ExchangeOrderId { get; private set; }
        public decimal Price { get; private set; }
        public decimal Quantity { get; private set; }
        public OrderSide Side { get; private set; }
        public OrderState State { get; private set; }

        protected GridOrder() { }

        public GridOrder(decimal price, decimal qty, OrderSide side)
        {
            Id = Guid.NewGuid();
            Price = price;
            Quantity = qty;
            Side = side;
            State = OrderState.New;
        }

        public void MarkFilled()
        {
            State = OrderState.Filled;
        }

        public void Cancel()
        {
            State = OrderState.Canceled;
        }
    }
}
