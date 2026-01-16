using StockAlert.interfaces;
using System.Text.Json.Serialization;

namespace StockAlert.models.dtos
{
    public class BrapiResponseDTO
    {
        [JsonPropertyName("results")]
        public required BrapiStockDTO[] Results { get; set; }
    }

    public class BrapiStockDTO : IStockInfo
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = "";

        [JsonPropertyName("regularMarketPrice")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal CurrentPrice { get; set; }

        [JsonPropertyName("regularMarketDayHigh")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal DayHigh { get; set; }

        [JsonPropertyName("regularMarketDayLow")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public required decimal DayLow { get; set; }
    }
}
