using System;
namespace StockAlert.models.configs
{
    public class StockMonitorConfig
    {
        public required bool StockRecommendation { get; set; }
        public required bool DayHighAlert { get; set; }
        public required bool DayLowAlert { get; set; }
    }
}
