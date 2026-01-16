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
    public class MqttVecnaProvider : IVecnaProvider
    {
        private readonly IMqttClientWrapper _mqtt_client;
        private readonly MqttVecnaConfig _config;
        private readonly ConcurrentQueue<IVecnaMessage> _messages_queue = new();
        private CancellationToken _cancellation_token;

        public MqttVecnaProvider(IOptions<MqttVecnaConfig> options, IMqttClientWrapper mqtt_client)
        {
            var config = options.Value;
            ValidateConfig(config);
            _config = config;
            _mqtt_client = mqtt_client;
        }

        public void ValidateConfig(MqttVecnaConfig cfg)
        {
            if (cfg is null)
                throw new ArgumentException("Configuração MqttVecnaConfig não pode ser nula.", nameof(cfg));
            if (string.IsNullOrWhiteSpace(cfg.VecnaTopic))
                throw new ArgumentException("Tópico MQTT para recebimento de mensagens do Vecna não configurado.", nameof(cfg.VecnaTopic));

        }

        public async Task Setup(CancellationToken cancelattion_token)
        {
            _cancellation_token = cancelattion_token;

            _mqtt_client.RegisterMessageHandler(OnMessageReceived);

            await _mqtt_client.SubscribeAsync(_config.VecnaTopic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, _cancellation_token);
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

            if (e.ApplicationMessage.Topic != _config.VecnaTopic)
                return Task.CompletedTask;

            Console.WriteLine($"[MqttVecnaProvider] Mensagem recebida no tópico '{e.ApplicationMessage.Topic}': {payload}");
            try
            {
                // Expecting payload to contain a JSON with Symbol field
                MqttVecnaMessageDTO? vecna_dto = JsonSerializer.Deserialize<MqttVecnaMessageDTO>(payload);
                if (vecna_dto == null)
                {
                    Console.WriteLine("[MqttVecnaProvider] Payload MQTT inválido: não foi possível desserializar para MqttVecnaMessageDTO.");
                    return Task.CompletedTask;
                }

                IVecnaMessage new_info = vecna_dto;
                _messages_queue.Enqueue(new_info);

                Console.WriteLine($"[MqttVecnaProvider] Informações registradas com sucesso no dicionário de informações de ação.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MqttVecnaProvider] Falha ao desserializar payload MQTT: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public List<IVecnaMessage> DrainMessageQueue()
        {
            List<IVecnaMessage> msg_list = new();
            
            while (true)
            {
                _messages_queue.TryDequeue(out var vecna_msg);
                if (vecna_msg == null)
                    break;

                msg_list.Add(vecna_msg);
            }

            return msg_list;
        }

        public Task<List<IVecnaMessage>?> GetVecnaMessages()
        {
            Console.WriteLine("[MqttVecnaProvider] Recuperando mensagens do Vecna...");

            List<IVecnaMessage> messages_from_queue = DrainMessageQueue();

            Console.WriteLine("[MqttVecnaProvider] Informações da ação recuperadas com sucesso.");
            return Task.FromResult<List<IVecnaMessage>?>(messages_from_queue);
        }
    }
}
