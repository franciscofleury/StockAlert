using Microsoft.Extensions.Options;
using MQTTnet;
using StockAlert.interfaces;
using StockAlert.models.configs;
using StockAlert.models.dtos;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace StockAlert.providers
{
    public class MqttStockProvider : IStockProvider
    {
        private readonly IMqttClientWrapper _mqtt_client;
        private readonly MqttStockConfig _config;
        private readonly ConcurrentDictionary<string, IStockInfo> _last_infos = new();
        private CancellationToken _cancellation_token;

        public MqttStockProvider(IOptions<MqttStockConfig> options, IMqttClientWrapper mqtt_client)
        {
            var config = options.Value;
            ValidateConfig(config);
            _config = config;
            _mqtt_client = mqtt_client;
        }

        public void ValidateConfig(MqttStockConfig cfg)
        {
            if (cfg is null)
                throw new ArgumentException("Configuração MqttStockConfig não pode ser nula.", nameof(cfg));
            if (string.IsNullOrWhiteSpace(cfg.StockTopic))
                throw new ArgumentException("Endereço do broker MQTT não configurado.", nameof(cfg.StockTopic));

        }

        public async Task Setup(CancellationToken cancelattion_token)
        {
            _cancellation_token = cancelattion_token;

            _mqtt_client.RegisterMessageHandler(OnMessageReceived);

            await _mqtt_client.SubscribeAsync(_config.StockTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, _cancellation_token);
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = e.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            if (string.IsNullOrWhiteSpace(payload))
                return Task.CompletedTask;

            if (e.ApplicationMessage == null)
                return Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(e.ApplicationMessage.Topic))
                return Task.CompletedTask;

            if (e.ApplicationMessage.Topic != _config.StockTopic)
                return Task.CompletedTask;

            Console.WriteLine($"[MqttStockProvider] Mensagem recebida no tópico '{e.ApplicationMessage.Topic}': {payload}");
            try
            {
                // Expecting payload to contain a JSON with Symbol field
                MqttStockDTO? stock_dto = JsonSerializer.Deserialize<MqttStockDTO>(payload);
                if (stock_dto == null)
                {
                    Console.WriteLine("[MqttStockProvider] Payload MQTT inválido: não foi possível desserializar para MqttAlertDTO.");
                    return Task.CompletedTask;
                }

                IStockInfo new_info = stock_dto;
                _last_infos[new_info.Symbol] = new_info;

                Console.WriteLine($"[MqttStockProvider] Informações registradas com sucesso no dicionário de informações de ação.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MqttStockProvider] Falha ao desserializar payload MQTT: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public Task<IStockInfo?> GetStockInfo(string stockSymbol)
        {
            Console.WriteLine("[MqttStockProvider] Recuperando informações da ação...");

            if (string.IsNullOrWhiteSpace(stockSymbol))
                return Task.FromResult<IStockInfo?>(null);

            if (!_last_infos.TryGetValue(stockSymbol, out var last_info))
                return Task.FromResult<IStockInfo?>(null);

            Console.WriteLine("[MqttStockProvider] Informações da ação recuperadas com sucesso.");
            return Task.FromResult<IStockInfo?>(last_info);
        }
    }
}
