using System;
namespace StockAlert.models.configs
{
    public class RandomStockConfig
    {
        public decimal StartPrice { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }
        public double StdStock { get; set; }
        public int AverageLatency { get; set; }
        public double StdLatency { get; set; }
        public double FailureRate { get; set; }
    }
}
