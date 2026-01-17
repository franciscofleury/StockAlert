using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.utils;

namespace StockAlert.services
{
    public class ConsoleAlertService : IAlertService
    {
        private readonly int _average_latency;
        private readonly double _std_latency;
        private readonly double _failure_rate;

        private CancellationToken _cancellation_token;
        public ConsoleAlertService(IOptions<ConsoleAlertConfig> options)
        {
            var config = options.Value;
            ValidateConfig(config);
            _average_latency = config.AverageLatency;
            _std_latency = config.StdLatency;
            _failure_rate = config.FailureRate;
        }

        private static void ValidateConfig(ConsoleAlertConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), "A configuração ConsoleMailConfig não pode ser nula.");

            if (config.AverageLatency < 0)
                throw new ArgumentException("AverageLatency deve ser maior ou igual a zero.", nameof(config.AverageLatency));

            if (config.StdLatency < 0)
                throw new ArgumentException("StdLatency deve ser maior ou igual a zero.", nameof(config.StdLatency));
        
            if (config.FailureRate < 0 || config.FailureRate > 1)
                throw new ArgumentException("FailureRate deve estar entre 0 e 1.", nameof(config.FailureRate));
        }

        public string GetName()
        {
            return "Console";
        }

        public Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            return Task.CompletedTask;
        }
        private int CalculateLatency()
        {
            // Usando a distribuição normal para calcular a latência
            int latency = Convert.ToInt32(BetterRandom.GetGaussianGeneratedNumber(_std_latency, _average_latency));

            return Math.Max(0, latency); // Garantir que a latência não seja negativa
        }

        private bool FailureChance()
        {
            Random rand = new Random();
            double chance = rand.NextDouble();
            return chance < _failure_rate;
        }

        public async Task<bool> SendAlert(Alert alert)
        {
            Console.WriteLine("[ConsoleAlertService] Enviando alerta via Console...");
            
            // Simulando latência no envio de alerta
            int latency_ms = CalculateLatency();

            await Task.Delay(latency_ms, _cancellation_token);
            
            // Calculando chance de falha
            if (FailureChance())
            {
                Console.WriteLine("[ConsoleAlertService] Falha no envio.");
                return false;
            }

            Console.WriteLine($"[ConsoleAlertService] Envio de alerta: (Assunto = {alert.Topic}, Mensagem = {alert.Message})");

            return true;
        }
    }
}
