using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.providers;
using System.Diagnostics.CodeAnalysis;

namespace StockAlert.workers
{
    public class AlertWorker : BackgroundService
    {
        private readonly IEnumerable<IAlertService> _alert_services;
        private readonly IHostApplicationLifetime _host_lifetime;
        private readonly Parameters _params;
        private readonly AlertQueue _monitor_worker_queue;
        private readonly bool _allow_alerting_failure;
        private readonly int _alert_loop_interval = 200; // valor em ms

        // Injeção de Dependência
        public AlertWorker(
            IOptions<WorkerConfig> options,
            IEnumerable<IAlertService> alert_services,
            IHostApplicationLifetime host_lifetime,
            Parameters alertParams,
            AlertQueue monitor_worker_queue
        )
        {
            var config = options.Value;
            ValidateConfig(config);
            _allow_alerting_failure = config.AllowAlertingFailure;
            _alert_services = alert_services;
            _params = alertParams;
            _monitor_worker_queue = monitor_worker_queue;
            _host_lifetime = host_lifetime;
        }
        private static void ValidateConfig(WorkerConfig cfg)
        {
            if (cfg.MonitorInterval <= 0)
                throw new ArgumentException("O MonitorInterval deve ser um valor positivo", nameof(cfg.MonitorInterval));
        }

        private async Task<bool> ProcessAlert(Alert alert)
        {
            foreach (IAlertService _alert_service in _alert_services)
            {
                Console.WriteLine($"[AlertWorker] Processando alerta via {_alert_service.GetName()}...");
                bool alertSuccess = await _alert_service.SendAlert(alert);
                if (alertSuccess)
                {
                    Console.WriteLine("[AlertWorker] Alerta processado com sucesso.");
                    continue;
                }
                if (_allow_alerting_failure)
                {
                    Console.WriteLine("[AlertWorker] Falha ao processar o alerta. Pulando...");
                    continue;
                }

                Console.WriteLine("[AlertWorker] Falha ao processar o alerta. Abortando...");
                return false;
            }

            return true;
        }

        public async Task SetupApplication(CancellationToken cancellation_token)
        {
            
            foreach (IAlertService service in _alert_services)
            {
                await service.Setup(cancellation_token);
            }

        }

        private void Shutdown()
        {

            Console.WriteLine("[AlertWorker] Finalizando worker...");
            _host_lifetime.StopApplication();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[AlertWorker] Pronto para receber alertas!");

            try
            {
                // Iniciando serviços de alerta
                await SetupApplication(stoppingToken);

                // Iniciando loop de alerta
                await foreach (var alert in _monitor_worker_queue.Reader.ReadAllAsync(stoppingToken))
                {
                    bool success = await ProcessAlert(alert);
                    if (!success)
                    {
                        Shutdown();
                    }

                    await Task.Delay(_alert_loop_interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[AlertWorker] Operação cancelada. Finalizando worker...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine($"[AlertWorker] Erro inesperados. Finalizando worker...");
                Shutdown();
            }

            return;
        }
    }
}
