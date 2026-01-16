using System.Text.Json.Serialization;

namespace StockAlert.models.dtos
{
    public class MqttAlertDTO
    {
        [JsonPropertyName("topic")]
        public required string Topic { get; set; }
        [JsonPropertyName("message")]
        public required string Message { get; set; }
    }
}
