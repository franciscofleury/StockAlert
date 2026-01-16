using System.Text.Json;
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.models.dtos;

namespace StockAlert.services
{
    public class MqttAlertService : IAlertService
    {
        private readonly MqttAlertConfig _config;
        private readonly IMqttClientWrapper _mqtt_client;

        private CancellationToken _cancellation_token;
        public MqttAlertService(IOptions<MqttAlertConfig> options, IMqttClientWrapper mqtt_client)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options), "A configuração MQTTConfig não pode ser nula.");
            ValidateConfig(_config);
            _mqtt_client = mqtt_client;
        }

        public string GetName() => "MQTT Alert";

        public Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            return Task.CompletedTask;
        }

        public async Task<bool> SendAlert(Alert alert)
        {
            Console.WriteLine("[MqttAlertService] Enviando alerta via MQTT...");

            try
            {
                string messagePayload = JsonSerializer.Serialize(new MqttAlertDTO { Topic = alert.Topic, Message = alert.Message});
                await _mqtt_client.PublishAsync(_config.AlertTopic, messagePayload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MqttAlertService] Erro ao enviar alerta MQTT: {ex.Message}");
                return false;
            }

            Console.WriteLine("[MqttAlertService] Alerta MQTT enviado com sucesso.");
            return true;
        }

        private static void ValidateConfig(MqttAlertConfig cfg)
        {
            if (cfg is null)
                throw new ArgumentNullException(nameof(cfg), "A configuração MQTTConfig não pode ser nula.");

            if (string.IsNullOrWhiteSpace(cfg.AlertTopic))
                throw new ArgumentException("AlertTopic não está configurado.", nameof(cfg.AlertTopic));
        }
    }
}