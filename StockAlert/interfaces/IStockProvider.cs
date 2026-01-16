namespace StockAlert.interfaces
{
    public interface IStockProvider
    {
        public Task Setup(CancellationToken cancellation_token);
        Task<IStockInfo?> GetStockInfo(string stockSymbol);
    }
}
