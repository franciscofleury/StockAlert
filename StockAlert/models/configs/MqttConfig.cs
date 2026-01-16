namespace StockAlert.models.configs
{
    public class MqttConfig
    {
        public required string Broker { get; set; }
        public int Port { get; set; }
        public required string ClientId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}