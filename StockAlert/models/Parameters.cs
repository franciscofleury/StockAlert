using System;
namespace StockAlert.models
{
    public class Parameters
    {
        public required string StockSymbol { get; set; }
        public required decimal BuyThreshold { get; set; }
        public required decimal SellThreshold { get; set; }
    }
}
