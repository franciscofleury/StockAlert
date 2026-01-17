using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.providers;
using StockAlert.services;
using StockAlert.workers;

namespace StockAlert.versions.tests
{
    public class VecnaMonitor_AllInOneTests
    {
        public class MqttVecnaProviderTest : IVersion
        {
            public static HostApplicationBuilder GenerateApplicationBuilder(Parameters alertParams)
            {

                HostApplicationBuilder builder = Host.CreateApplicationBuilder();

                // Recuperando configurações
                builder.Services.Configure<MqttConfig>(builder.Configuration.GetSection("MqttConfig"));
                builder.Services.Configure<MqttVecnaConfig>(builder.Configuration.GetSection("MqttVecnaConfig"));
                builder.Services.Configure<ConsoleAlertConfig>(builder.Configuration.GetSection("ConsoleAlertConfig"));
                builder.Services.Configure<WorkerConfig>(builder.Configuration.GetSection("WorkerConfig"));

                // Registro de serviços (injeção de dependência) 
                builder.Services.AddSingleton(alertParams);
                builder.Services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();

                builder.Services.AddTransient<IMonitorService, VecnaMonitorService>();
                builder.Services.AddTransient<IAlertService, ConsoleAlertService>();

                builder.Services.AddTransient<IVecnaProvider, MqttVecnaProvider>();

                // Registro de worker
                builder.Services.AddHostedService<AllInOneWorker>();

                return builder;
            }
        }

        public class SmtpAlertServiceTest : IVersion
        {
            public static HostApplicationBuilder GenerateApplicationBuilder(Parameters alertParams)
            {

                HostApplicationBuilder builder = Host.CreateApplicationBuilder();

                // Recuperando configurações do servidor SMTP   
                builder.Services.Configure<MqttConfig>(builder.Configuration.GetSection("MqttConfig"));
                builder.Services.Configure<MqttVecnaConfig>(builder.Configuration.GetSection("MqttVecnaConfig"));
                builder.Services.Configure<SmtpConfig>(builder.Configuration.GetSection("SmtpConfig"));
                builder.Services.Configure<WorkerConfig>(builder.Configuration.GetSection("WorkerConfig"));

                // Registro de serviços (injeção de dependência) e configuração do Worker (responsável pelo monitoramento contínuo)
                builder.Services.AddSingleton(alertParams);
                builder.Services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();

                builder.Services.AddTransient<IMonitorService, VecnaMonitorService>();
                builder.Services.AddTransient<IVecnaProvider, MqttVecnaProvider>();
                builder.Services.AddTransient<IAlertService, SmtpAlertService>();

                // 4. Registrar o Worker
                builder.Services.AddHostedService<AllInOneWorker>();

                return builder;
            }
        }

        public class MqttAlertServiceTest : IVersion
        {
            public static HostApplicationBuilder GenerateApplicationBuilder(Parameters alertParams)
            {
                HostApplicationBuilder builder = Host.CreateApplicationBuilder();
                // Recuperando configurações do servidor MQTT   
                builder.Services.Configure<MqttConfig>(builder.Configuration.GetSection("MqttConfig"));
                builder.Services.Configure<MqttAlertConfig>(builder.Configuration.GetSection("MqttAlertConfig"));
                builder.Services.Configure<MqttVecnaConfig>(builder.Configuration.GetSection("MqttVecnaConfig"));
                builder.Services.Configure<WorkerConfig>(builder.Configuration.GetSection("WorkerConfig"));

                // Registro de serviços (injeção de dependência) e configuração do Worker (responsável pelo monitoramento contínuo)
                builder.Services.AddSingleton(alertParams);
                builder.Services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();

                builder.Services.AddTransient<IMonitorService, VecnaMonitorService>();
                builder.Services.AddTransient<IVecnaProvider, MqttVecnaProvider>();
                builder.Services.AddTransient<IAlertService, MqttAlertService>();
                // 4. Registrar o Worker
                builder.Services.AddHostedService<AllInOneWorker>();
                return builder;
            }
        }

        public class MultipleAlertsTest : IVersion
        {
            public static HostApplicationBuilder GenerateApplicationBuilder(Parameters alertParams)
            {

                HostApplicationBuilder builder = Host.CreateApplicationBuilder();


                // Recuperando configurações
                builder.Services.Configure<MqttConfig>(builder.Configuration.GetSection("MqttConfig"));
                builder.Services.Configure<MqttVecnaConfig>(builder.Configuration.GetSection("MqttVecnaConfig"));
                builder.Services.Configure<MqttAlertConfig>(builder.Configuration.GetSection("MqttAlertConfig"));
                builder.Services.Configure<ConsoleAlertConfig>(builder.Configuration.GetSection("ConsoleAlertConfig"));
                builder.Services.Configure<WorkerConfig>(builder.Configuration.GetSection("WorkerConfig"));
                builder.Services.Configure<SmtpConfig>(builder.Configuration.GetSection("SmtpConfig"));

                // Registro de serviços (injeção de dependência) e configuração do Worker (responsável pelo monitoramento contínuo)
                builder.Services.AddSingleton(alertParams);
                builder.Services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();

                builder.Services.AddTransient<IMonitorService, VecnaMonitorService>();
                builder.Services.AddTransient<IVecnaProvider, MqttVecnaProvider>();
                builder.Services.AddTransient<IAlertService, SmtpAlertService>();
                builder.Services.AddTransient<IAlertService, MqttAlertService>();
                builder.Services.AddTransient<IAlertService, ConsoleAlertService>();

                // 4. Registrar o Worker
                builder.Services.AddHostedService<AllInOneWorker>();

                return builder;
            }
        }
    }
}
