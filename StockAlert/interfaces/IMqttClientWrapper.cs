using MQTTnet;
using MQTTnet.Protocol;

public interface IMqttClientWrapper : IAsyncDisposable
{
    bool IsConnected { get; }
    Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, CancellationToken cancellationToken = default);
    void RegisterMessageHandler(Func<MqttApplicationMessageReceivedEventArgs, Task> handler);
}