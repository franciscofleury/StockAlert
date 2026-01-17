using StockAlert.interfaces;
using System;
using System.Text.Json.Serialization;

namespace StockAlert.models.dtos
{
    public class MqttVecnaMessageDTO : IVecnaMessage
    {
        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }
}
