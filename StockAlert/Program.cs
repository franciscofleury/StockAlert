using Microsoft.Extensions.Hosting;
using StockAlert.models;
using StockAlert.versions;
using StockAlert.versions.tests;
using System.Globalization;

namespace StockAlert
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Símbolos de ação aceitos
            string[] accepted_symbols = new string[] { "PETR4", "VALE3", "ITUB4", "MGLU3" };

            // Verificando parâmetros de entrada
            if (args.Length != 3)
                throw new Exception("Uso: StockAlert <SIMBOLO_ACAO> <LIMIAR_COMPRA> <LIMIAR_VENDA>");

            if (accepted_symbols.Contains(args[0]) == false)
                throw new Exception($"Símbolo de ação inválido. Símbolos aceitos: {string.Join(", ", accepted_symbols)}");

            decimal buy_threshold;
            decimal sell_threshold;

            // Apenas aceita números com ponto como separador decimal (ex: 74.8)
            bool sellParsed = decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out sell_threshold);
            bool buyParsed = decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out buy_threshold);

            if (!buyParsed || !sellParsed)
            {
                throw new Exception("Limiar inválido. Deve ser um número decimal com ponto como separador decimal (ex: 74.8).");
            }

            Parameters alertParameters = new Parameters { StockSymbol = args[0], BuyThreshold = buy_threshold, SellThreshold = sell_threshold };

            // -----------------------------------------------------------------------------------------------------------

            // TESTES

            // -----------------------------------------------------------------------------------------------------------

            // Testes do serviço de monitoramento B3MonitorService com AllInOne worker

            //     Teste de infraestrutura
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.InfrastructureTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do BrapiStockProvider
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.BrapiProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttStockProvider
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.MqttStockProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do SmtpAlertService
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.SmtpAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttAlertService
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.MqttAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste de múltiplos sistemas de alerta
            //HostApplicationBuilder builder = B3Monitor_AllInOneTests.MultipleAlertsTest.GenerateApplicationBuilder(alertParameters);


            // -----------------------------------------------------------------------------------------------------------

            // Testes do serviço de monitoramento B3MonitorService com Monitor worker e Alert worker

            //     Teste de infraestrutura
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.InfrastructureTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do BrapiStockProvider
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.BrapiProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttStockProvider
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.MqttProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do SmtpMailService
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.SmtpAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttAlertService
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.MqttAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste de múltiplos sistemas de alerta
            //HostApplicationBuilder builder = B3Monitor_TwoWorkersTests.MultipleAlertsTest.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            // Testes do serviço de monitoramento VecnaMonitorService com AllInOne worker

            //     Teste do MqttVecnaProvider
            //HostApplicationBuilder builder = VecnaMonitor_AllInOneTests.MqttVecnaProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do SmtpAlertService
            //HostApplicationBuilder builder = VecnaMonitor_AllInOneTests.SmtpAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttAlertService
            //HostApplicationBuilder builder = VecnaMonitor_AllInOneTests.MqttAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste de múltiplos sistemas de alerta
            //HostApplicationBuilder builder = VecnaMonitor_AllInOneTests.MultipleAlertsTest.GenerateApplicationBuilder(alertParameters);


            // -----------------------------------------------------------------------------------------------------------

            // Testes do serviço de monitoramento VecnaMonitorServices com Monitor worker e Alert worker

            //     Teste do MqttVecnaProvider
            //HostApplicationBuilder builder = VecnaMonitor_TwoWorkersTests.MqttVecnaProviderTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do SmtpMailService
            //HostApplicationBuilder builder = VecnaMonitor_TwoWorkersTests.SmtpAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste do MqttAlertService
            //HostApplicationBuilder builder = VecnaMonitor_TwoWorkersTests.MqttAlertServiceTest.GenerateApplicationBuilder(alertParameters);

            //     Teste de múltiplos sistemas de alerta
            //HostApplicationBuilder builder = VecnaMonitor_TwoWorkersTests.MultipleAlertsTest.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            // VERSÕES FINAIS

            // -----------------------------------------------------------------------------------------------------------

            // Versão com BrapiStockProvider como provedor de ações e AllInOneWorker como worker
            //HostApplicationBuilder builder = Brapi_AllInOne.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            // Versão com BrapiStockProvider como provedor de ações, MonitorWorker como worker de monitoramento e AlertWorker como worker de alerta
            //HostApplicationBuilder builder = Brapi_TwoWorkers.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            // Versão com RandomStockProvider como provedor de ações e AllInOneWorker como worker
            //HostApplicationBuilder builder = Random_AllInOne.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            // Versão com RandomStockProvider como provedor de ações, MonitorWorker como worker de monitoramento e AlertWorker como worker de alerta
            HostApplicationBuilder builder = Random_TwoWorkers.GenerateApplicationBuilder(alertParameters);

            // -----------------------------------------------------------------------------------------------------------

            using IHost host = builder.Build();

            await host.RunAsync();
        }
    }
}
