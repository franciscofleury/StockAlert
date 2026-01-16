namespace StockAlert.models.configs
{
    public class WorkerConfig
    {
        public int MonitorInterval { get; set; } = 5000;
        public bool AllowMonitoringFailure { get; set; }
        public bool AllowAlertingFailure { get; set; }
    }
}
