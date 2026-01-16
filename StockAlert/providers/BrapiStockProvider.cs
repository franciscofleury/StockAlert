using StockAlert.interfaces;
using StockAlert.models.dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace StockAlert.providers
{
    public class BrapiStockProvider : IStockProvider
    {
        private readonly HttpClient _httpClient;
        private CancellationToken _cancellation_token;

        public BrapiStockProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            return;
        }

        public async Task<IStockInfo?> GetStockInfo(string stockSymbol)
        {
            Console.WriteLine("[BrapiStockProvider] Recuperando informações da ação...");

            var url = $"https://brapi.dev/api/quote/{Uri.EscapeDataString(stockSymbol)}";

            // Pedindo dados à API Brapi
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url, _cancellation_token);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[BrapiStockProvider] Erro no request HTTP: {ex}");
                return null;
            }

            // Desserializando a resposta da Brapi
            IStockInfo stock_info;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                BrapiResponseDTO? results = await response.Content.ReadFromJsonAsync<BrapiResponseDTO>(options, _cancellation_token);

                if (results == null || results.Results == null)
                    throw new InvalidOperationException("Resposta inválida da API Brapi.");
                stock_info = results.Results.Length > 0 ? results.Results[0] : throw new InvalidOperationException("Nenhum dado de ação encontrado para o símbolo fornecido.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrapiStockProvider] Erro ao desserializar a resposta: {ex}");
                return null;
            }

            Console.WriteLine("[BrapiStockProvider] Informações da ação recuperadas com sucesso.");
            return stock_info;
        }
    }
}