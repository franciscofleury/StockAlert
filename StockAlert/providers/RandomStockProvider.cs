
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models.configs;
using StockAlert.utils;

namespace StockAlert.providers
{
    public class StockInfo : IStockInfo
    {
        public string Symbol { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }

    }
    public class RandomStockProvider : IStockProvider
    {
        private decimal _stock_price;
        private double _std_stock;
        private decimal _day_high;
        private decimal _day_low;
        private int _average_latency;
        private double _std_latency;
        private double _failure_rate;
        private DateTime _last_stock_check = DateTime.Now;
        private CancellationToken _cancellation_token;
        public RandomStockProvider(IOptions<RandomStockConfig> options)
        {
            var config = options.Value;
            ValidateConfig(config);
            _stock_price = config.StartPrice;
            _std_stock = config.StdStock;
            _day_high = config.DayHigh;
            _day_low = config.DayLow;
            _average_latency = config.AverageLatency;
            _std_latency = config.StdLatency;
            _failure_rate = config.FailureRate;
        }

        // Validações básicas da configuração do gerador de preços aleatórios
        private static void ValidateConfig(RandomStockConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), "A configuração RandomStockConfig não pode ser nula.");

            if (config.StartPrice < 0m)
                throw new ArgumentException("StartPrice deve ser maior ou igual a zero.", nameof(config.StartPrice));

            if (config.DayLow < 0m)
                throw new ArgumentException("DayLow deve ser maior ou igual a zero.", nameof(config.DayLow));

            if (config.DayHigh < 0m)
                throw new ArgumentException("DayHigh deve ser maior ou igual a zero.", nameof(config.DayHigh));

            if (config.DayHigh < config.DayLow)
                throw new ArgumentException("DayHigh deve ser maior ou igual a DayLow.", nameof(config.DayHigh));

            if (config.StartPrice < config.DayLow || config.StartPrice > config.DayHigh)
                throw new ArgumentException("StartPrice deve estar entre DayLow e DayHigh.", nameof(config.StartPrice));

            if (config.StdStock < 0.0)
                throw new ArgumentException("StdStock deve ser maior ou igual a zero.", nameof(config.StdStock));

            if (config.AverageLatency < 0)
                throw new ArgumentException("AverageLatency deve ser maior ou igual a zero.", nameof(config.AverageLatency));
            
            if (config.StdLatency < 0.0)
                throw new ArgumentException("StdLatency deve ser maior ou igual a zero.", nameof(config.StdLatency));

            if (config.FailureRate < 0.0 || config.FailureRate > 1.0)
                throw new ArgumentException("Failure deve estar entre 0.0 e 1.0.", nameof(config.FailureRate));
        }

        public async Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            return;
        }

        private int CalculateLatency()
        {
            // Usando a distribuição normal para calcular a latência
            int latency = Convert.ToInt32(BetterRandom.GetGaussianGeneratedNumber(_std_latency, _average_latency));

            return Math.Max(0, latency); // Garantir que a latência não seja negativa
        }

        private void CalculateNewStockPrice()
        {
            TimeSpan seconds_since_last_calc = DateTime.Now - _last_stock_check;
            double variation_per_second = BetterRandom.GetGaussianGeneratedNumber(_std_stock);
            _stock_price += ((decimal)variation_per_second * (decimal)seconds_since_last_calc.TotalSeconds);

            _stock_price = Math.Max(0, _stock_price);
            _day_high = Math.Max(_stock_price, _day_high);
            _day_low = Math.Min(_stock_price, _day_low);

            _last_stock_check = DateTime.Now;
        }


        private bool FailureChance()
        {
            Random rand = new Random();
            double chance = rand.NextDouble();
            return chance < _failure_rate;
        }
        public async Task<IStockInfo?> GetStockInfo(string stockSymbol)
        {
            Console.WriteLine("[RandomStockProvider] Recuperando informações da ação...");

            // Calcular latência
            int latency = CalculateLatency();

            await Task.Delay(latency, _cancellation_token);

            // Calcular novo preço de ação
            CalculateNewStockPrice();

            // Calcular chance de falha
            if (FailureChance())
            {
                Console.WriteLine("[RandomStockProvider] Falha ao recuperar valor da ação.");
                return null;
            }

            // Envio de novas informações
            IStockInfo? result = new StockInfo
            {
                Symbol = stockSymbol,
                CurrentPrice = _stock_price,
                DayHigh = _day_high,
                DayLow = _day_low
            };

            Console.WriteLine($"[RandomStockProvider] Novo valor de ação gerado {stockSymbol}: {_stock_price}");

            return result;
        }
    }
}