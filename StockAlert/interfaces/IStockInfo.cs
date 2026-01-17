namespace StockAlert.interfaces
{
    public interface IStockInfo
    {
        public string Symbol { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }
    }
}

