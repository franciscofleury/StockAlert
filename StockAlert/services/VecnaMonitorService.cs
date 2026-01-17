using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;

namespace StockAlert.services
{
    public class VecnaMonitorService : IMonitorService
    {
        private readonly Parameters _params;
        private readonly IVecnaProvider _vecna_provider;

        private CancellationToken _cancellation_token;
        public VecnaMonitorService(IOptions<StockMonitorConfig> options, Parameters alertParameters, IVecnaProvider vecna_provider)
        {
            var config = options.Value;
            _params = alertParameters;
            _vecna_provider = vecna_provider;
        }

        public string GetName()
        {
            return "B3 Stock";
        }

        public async Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            await _vecna_provider.Setup(_cancellation_token);

            return;
        }
 
        public async Task<List<Alert>?> Monitor()
        {
            List<IVecnaMessage>? vecna_messages = await _vecna_provider.GetVecnaMessages();

            if (vecna_messages == null)
                return null;

            List<Alert> alert_messages = vecna_messages.Select(m => new Alert("Alerta de mensagem do Vecna!", m.Content)).ToList();

            Console.WriteLine($"[VecnaMonitorService] {alert_messages.Count} novos alertas do Vecna gerados.");

            return alert_messages;
        }
    }
}
