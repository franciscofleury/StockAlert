using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.providers;
using StockAlert.services;
using System.Diagnostics.CodeAnalysis;

namespace StockAlert.workers
{
    public class AllInOneWorker : BackgroundService
    {
        private readonly IEnumerable<IMonitorService> _monitor_services;
        private readonly IEnumerable<IAlertService> _alert_services;
        private readonly IHostApplicationLifetime _host_lifetime;
        private readonly Parameters _params;
        private readonly int _monitor_interval;
        private readonly bool _allow_monitoring_failure;
        private readonly bool _allow_alerting_failure;

        // Injeção de Dependência
        public AllInOneWorker(
            IOptions<WorkerConfig> options,
            IEnumerable<IMonitorService> monitor_services, 
            IEnumerable<IAlertService> alert_services, 
            IHostApplicationLifetime host_lifetime, 
            Parameters alertParams
        )
        {
            var config = options.Value;
            ValidateConfig(config);
            _monitor_interval = config.MonitorInterval;
            _allow_monitoring_failure = config.AllowMonitoringFailure;
            _allow_alerting_failure = config.AllowAlertingFailure;
            _monitor_services = monitor_services;
            _alert_services = alert_services;
            _params = alertParams;
            _host_lifetime = host_lifetime;
        }
        private static void ValidateConfig(WorkerConfig cfg)
        {
            if (cfg.MonitorInterval <= 0)
                throw new ArgumentException("O MonitorInterval deve ser um valor positivo", nameof(cfg.MonitorInterval));
        }

        private async Task<List<Alert>?> ProcessMonitoring()
        {
            Console.WriteLine("[AllInOneWorker] Processando monitoramentos...");

            List<Alert> alertsToSend = new List<Alert>();
            foreach (IMonitorService monitorService in _monitor_services)
            {
                List<Alert>? montiroAlerts = await monitorService.Monitor();
                if (montiroAlerts != null)
                {
                    Console.WriteLine($"[AllInOneWorker] Monitoramento de {monitorService.GetName()} processado com sucesso.");
                    alertsToSend.AddRange(montiroAlerts);
                    continue;
                }
                if (_allow_monitoring_failure)
                {
                    Console.WriteLine($"[AllInOneWorker] Falha ao processar o monitoramento de {monitorService.GetName()}. Pulando...");
                    continue;
                }

                Console.WriteLine($"[AllInOneWorker] Falha ao processar o monitoramento de {monitorService.GetName()}. Abortando...");
                return null;
            }

            return alertsToSend;
        }
        private async Task<bool> ProcessAlert(Alert alert)
        {
            foreach (IAlertService _alert_service in _alert_services)
            {
                Console.WriteLine($"[AllInOneWorker] Processando alerta via {_alert_service.GetName()}...");
                bool alertSuccess = await _alert_service.SendAlert(alert);
                if (alertSuccess)
                {
                    continue;
                }
                if (_allow_alerting_failure)
                {
                    Console.WriteLine("[AllInOneWorker] Falha ao processar o alerta. Pulando...");
                    continue;
                }

                Console.WriteLine("[AllInOneWorker] Falha ao processar o alerta. Abortando...");
                return false;
            }
            
            return true;
        }

        private async Task<bool> ProcessAlertList(List<Alert> alerts)
        {
            Console.WriteLine($"[AllInOneWorker] Processando lista de {alerts.Count} alertas...");

            int i = 1;
            foreach (Alert alert in alerts)
            {
                bool success = await ProcessAlert(alert);
                if (!success)
                    return false;

                Console.WriteLine($"[AllInOneWorker] Alerta {i++}/{alerts.Count} processado com sucesso.");
            }

            return true;
        }

        public async Task SetupApplication(CancellationToken cancellation_token)
        {
            foreach (IAlertService service in _alert_services)
            {
                await service.Setup(cancellation_token);
            }
            
            foreach(IMonitorService service in _monitor_services)
            {
                await service.Setup(cancellation_token);
            }
        }

        private void Shutdown()
        {
 
            Console.WriteLine("[AllInOneWorker] Finalizando worker...");
            _host_lifetime.StopApplication();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[AllInOneWorker] Monitorando ação {_params.StockSymbol} com limiar de compra {_params.BuyThreshold:F6} e limiar de venda {_params.SellThreshold:F6}");


            try
            {
                // Iniciando serviços e providers
                await SetupApplication(stoppingToken);
                
                // Iniciando loop principal da aplicação
                while (!stoppingToken.IsCancellationRequested)
                {
                    List<Alert>? alertsToSend = await ProcessMonitoring();

                    if (alertsToSend == null)
                    {
                        Shutdown();
                        return;
                    }
                
                    bool success = await ProcessAlertList(alertsToSend);
                    if (!success)
                    {
                        Shutdown();
                        return;
                    }

                    await Task.Delay(_monitor_interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[AllInOneWorker] Operação cancelada. Finalizando worker...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("[AllInOneWorker] Exceção inesperado. Abortando...");
                Shutdown();
            }

            return;
        }
    }
}
