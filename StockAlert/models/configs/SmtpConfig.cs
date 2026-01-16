namespace StockAlert.models.configs
{
    public class SmtpConfig
    {
        public required string Server { get; set; }
        public required int Port { get; set; }
        public required string SenderAddress { get; set; }
        public required string SenderPassword { get; set; }
        public required string TargetAddress { get; set; }
        public required int MaxTries { get; set; }
    }
}
