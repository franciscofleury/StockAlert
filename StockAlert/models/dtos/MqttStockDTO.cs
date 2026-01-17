using StockAlert.interfaces;
using System.Text.Json.Serialization;

namespace StockAlert.models.dtos
{
    public class MqttStockDTO : IStockInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = "";

        [JsonPropertyName("price")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal CurrentPrice { get; set; }

        [JsonPropertyName("dayHigh")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal DayHigh { get; set; }

        [JsonPropertyName("dayLow")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal DayLow { get; set; }
    }
}
