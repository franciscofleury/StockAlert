using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using StockAlert.models.configs;

namespace StockAlert.services
{
    public class MqttClientWrapper : IMqttClientWrapper
    {
        private readonly MqttConfig _config;
        private readonly IMqttClient _client;
        private readonly MqttClientFactory _factory;
        private readonly MqttClientOptions _options;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly ConcurrentBag<Func<MqttApplicationMessageReceivedEventArgs, Task>> _handlers = new();

        public bool IsConnected => _client?.IsConnected ?? false;

        public MqttClientWrapper(IOptions<MqttConfig> options)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options), "A configuração MQTTConfig não pode ser nula.");
            ValidateConfig(_config);

            _factory = new MqttClientFactory();
            _client = _factory.CreateMqttClient();

            var builder = new MqttClientOptionsBuilder()
                .WithClientId(_config.ClientId)
                .WithTcpServer(_config.Broker, _config.Port);

            if (!string.IsNullOrWhiteSpace(_config.Username))
                builder.WithCredentials(_config.Username, _config.Password ?? string.Empty);

            _options = builder.Build();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                Console.WriteLine("[MqttClientWrapper] Mensagem recebida. Encaminhando à handlers...");
                foreach (var h in _handlers)
                {
                    try
                    {
                        await h(e).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MqttClientWrapper] Handler falhou: {ex.Message}");
                    }
                }
            };
        }

        public async Task ConnectIfNeededAsync(CancellationToken cancellationToken = default)
        {
            if (_client.IsConnected)
                return;

            await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_client.IsConnected)
                    return;

                await _client.ConnectAsync(_options, cancellationToken).ConfigureAwait(false);
                Console.WriteLine("[MqttClientWrapper] Conectado ao broker MQTT.");
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default)
        {
            await ConnectIfNeededAsync(cancellationToken).ConfigureAwait(false);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload ?? string.Empty))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            await _client.PublishAsync(message, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"[MqttClientWrapper] Mensagem publicada no tópico '{topic}'.");
        }

        public async Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, CancellationToken cancellationToken = default)
        {
            await ConnectIfNeededAsync(cancellationToken).ConfigureAwait(false);
            var mqttSubscribeOptions = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(qos)
                .Build();
            await _client.SubscribeAsync(mqttSubscribeOptions, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"[MqttClientWrapper] Cliente inscrito em novo tópico '{topic}'");
        }

        public void RegisterMessageHandler(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers.Add(handler);
        }

        private static void ValidateConfig(MqttConfig cfg)
        {
            if (cfg is null)
                throw new ArgumentNullException(nameof(cfg), "A configuração MQTTConfig não pode ser nula.");

            if (string.IsNullOrWhiteSpace(cfg.Broker))
                throw new ArgumentException("Broker MQTT não está configurado.", nameof(cfg.Broker));

            if (cfg.Port <= 0)
                throw new ArgumentException("Porta MQTT inválida. Deve ser maior que zero.", nameof(cfg.Port));

            if (string.IsNullOrWhiteSpace(cfg.ClientId))
                throw new ArgumentException("ClientId MQTT não pode ser vazio.", nameof(cfg.ClientId));
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_client != null && _client.IsConnected)
                    await _client.DisconnectAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignorar erros na desconexão
            }
            _connectLock.Dispose();
        }
    }
}