using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Aggregates.Strategy
{
    public class IndicatorResult
    {
        public string IndicatorName { get; }
        public decimal Value { get; }
        public DateTime Time { get; }

        public IndicatorResult(string name, decimal value)
        {
            IndicatorName = name;
            Value = value;
            Time = DateTime.UtcNow;
        }
    }
}
