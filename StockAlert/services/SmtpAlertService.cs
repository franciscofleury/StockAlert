using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;

namespace StockAlert.services
{
    public class SmtpAlertService : IAlertService
    {
        private readonly SmtpConfig _config;

        private CancellationToken _cancellation_token;
        public SmtpAlertService(IOptions<SmtpConfig> options)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            ValidateConfig(_config);
        }
        private static void ValidateConfig(SmtpConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.Server))
                throw new ArgumentException("Servidor SMTP não configurado.", nameof(cfg.Server));
            if (cfg.Port <= 0)
                throw new ArgumentException("Porta SMTP configurada inválida.", nameof(cfg.Port));
            if (string.IsNullOrWhiteSpace(cfg.SenderAddress))
                throw new ArgumentException("Endereço do remetente não configurado.", nameof(cfg.SenderAddress));
            if (string.IsNullOrWhiteSpace(cfg.SenderPassword))
                throw new ArgumentException("Senha do remetente não configurada.", nameof(cfg.SenderPassword));
            if (string.IsNullOrWhiteSpace(cfg.TargetAddress))
                throw new ArgumentException("Endereço do destinatário não configurado.", nameof(cfg.TargetAddress));
            if (cfg.MaxTries <= 0)
                throw new ArgumentException("Número máximo de tentativas deve ser um valor positivo.", nameof(cfg.MaxTries));
        }

        public string GetName()
        {
            return "SMTP";
        }

        public Task Setup(CancellationToken cancellation_token)
        {
            _cancellation_token = cancellation_token;

            return Task.CompletedTask;
        }
        public async Task<bool> SendAlert(Alert alert)
        {
            Console.WriteLine("[SMTPAlertService] Enviando alerta via SMTP...");
            using var message = new MailMessage();
            message.From = new MailAddress(_config.SenderAddress);
            message.To.Add(_config.TargetAddress);
            message.Subject = alert.Topic ?? string.Empty;
            message.Body = alert.Message ?? string.Empty;
            message.IsBodyHtml = false;

            using var client = new SmtpClient(_config.Server, _config.Port)
            {
                Credentials = new NetworkCredential(_config.SenderAddress, _config.SenderPassword),
                EnableSsl = true // adjust if your SMTP server requires no SSL or STARTTLS specifics
            };

            int tries = 0;
            while (tries < _config.MaxTries)
            {
                try
                {
                    await client.SendMailAsync(message, _cancellation_token);
                    Console.WriteLine("[SMTPAlertService] Alerta via SMTP enviado com sucesso.");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SMTPAlertService] Falha ao enviar alerta via SMTP. Tentativa {++tries} de {_config.MaxTries}.");
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("[SMTPAlertService] Falha ao enviar alerta via SMTP após múltiplas tentativas. Abortando...");
            return false;
        }
    }
}