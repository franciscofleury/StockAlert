using StockAlert.models;

namespace StockAlert.interfaces
{
    public interface IAlertService
    {
        public Task Setup(CancellationToken cancellation_token);
        public string GetName();
        public Task<bool> SendAlert(Alert alert);
    }
}
