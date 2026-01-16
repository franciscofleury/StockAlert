namespace StockAlert.interfaces
{
    public interface IVecnaProvider
    {
        public Task Setup(CancellationToken cancellation_token);
        public Task<List<IVecnaMessage>?> GetVecnaMessages();
    }
}
