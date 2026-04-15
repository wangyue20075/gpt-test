using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Aggregates.Grid
{
    public class GridLevel
    {
        public int Index { get; private set; }
        public decimal Price { get; private set; }

        public GridLevel(decimal price, int index)
        {
            Price = price;
            Index = index;
        }
    }
}
