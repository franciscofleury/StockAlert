using StockAlert.models;

namespace StockAlert.interfaces
{
    public interface IMonitorService
    {
        public Task Setup(CancellationToken cancellation_token);
        public string GetName();
        public Task<List<Alert>?> Monitor();
    }
}
