using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;

namespace StockAlert.workers
{
    public class MonitorWorker : BackgroundService
    {
        private readonly IEnumerable<IMonitorService> _monitor_services;
        private readonly IHostApplicationLifetime _host_lifetime;
        private readonly Parameters _params;
        private readonly AlertQueue _alertMonitorQueue;
        private readonly int _monitor_interval;
        private readonly bool _allow_monitoring_failure;

        // Injeção de Dependência
        public MonitorWorker(IOptions<WorkerConfig> options, IEnumerable<IMonitorService> monitor_services, IEnumerable<IAlertService> alert_services, IHostApplicationLifetime host_lifetime, Parameters alertParams, AlertQueue alertMonitorQueue)
        {
            var config = options.Value;
            ValidateConfig(config);
            _monitor_interval = config.MonitorInterval;
            _allow_monitoring_failure = config.AllowMonitoringFailure;
            _monitor_services = monitor_services;
            _params = alertParams;
            _alertMonitorQueue = alertMonitorQueue;
            _host_lifetime = host_lifetime;
        }
        private static void ValidateConfig(WorkerConfig cfg)
        {
            if (cfg.MonitorInterval <= 0)
                throw new ArgumentException("O MonitorInterval deve ser um valor positivo", nameof(cfg.MonitorInterval));
        }

        private async Task<List<Alert>?> ProcessMonitoring()
        {
            Console.WriteLine("[MonitorWorker] Processando monitoramentos...");

            List<Alert> alertsToSend = new List<Alert>();
            foreach (IMonitorService monitorService in _monitor_services)
            {
                List<Alert>? montiroAlerts = await monitorService.Monitor();
                if (montiroAlerts != null)
                {
                    Console.WriteLine($"[MonitorWorker] Monitoramento de {monitorService.GetName()} processado com sucesso.");
                    alertsToSend.AddRange(montiroAlerts);
                    continue;
                }
                if (_allow_monitoring_failure)
                {
                    Console.WriteLine($"[MonitorWorker] Falha ao processar o monitoramento de {monitorService.GetName()}. Pulando...");
                    continue;
                }

                Console.WriteLine($"[MonitorWorker] Falha ao processar o monitoramento de {monitorService.GetName()}. Abortando...");
                return null;
            }

            return alertsToSend;
        }

        public async Task SetupApplication(CancellationToken cancellation_token)
        {

            foreach (IMonitorService service in _monitor_services)
            {
                await service.Setup(cancellation_token);
            }

        }

        private void Shutdown()
        {

            Console.WriteLine("[AllInOneWorker] Finalizando worker...");
            _host_lifetime.StopApplication();
        }

        private async Task SendToAlertMonitor(List<Alert> alerts)
        {
            Console.WriteLine($"[MonitorWorker] Enviando {alerts.Count} alertas para o AlertMonitorWorker...");
            int i = 1;
            foreach (Alert alert in alerts)
            {
                await _alertMonitorQueue.Writer.WriteAsync(alert);
                Console.WriteLine($"[MonitorWorker] Alerta {i}/{alerts.Count} enviado para o AlertMonitorWorker.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[MonitorWorker] Monitorando ação {_params.StockSymbol} com limiar de compra {_params.BuyThreshold} e limiar de venda {_params.SellThreshold}");

            try
            {
                // Iniciando serviços e providers necessários para monitoramento
                await SetupApplication(stoppingToken);

                // Iniciando loop de monitoramento
                while (!stoppingToken.IsCancellationRequested)
                {
                    List<Alert>? alertsToSend = await ProcessMonitoring();

                    if (alertsToSend == null)
                    {
                        Shutdown();
                        return;
                    }

                    await SendToAlertMonitor(alertsToSend);

                    await Task.Delay(_monitor_interval, stoppingToken);
                }
            } catch (OperationCanceledException)
            {
                Console.WriteLine("[MonitorWorker] Operação cancelada. Finalizando worker...");
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("[MonitorWorker] Exceção inesperado. Abortando...");
                Shutdown();
            }

            return;
        }
    }
}
