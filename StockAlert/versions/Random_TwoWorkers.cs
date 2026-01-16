using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockAlert.interfaces;
using StockAlert.models;
using StockAlert.models.configs;
using StockAlert.providers;
using StockAlert.services;
using StockAlert.workers;

namespace StockAlert.versions
{
    public class Random_TwoWorkers : IVersion
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
            builder.Services.Configure<RandomStockConfig>(builder.Configuration.GetSection("RandomStockConfig"));
            builder.Services.Configure<StockMonitorConfig>(builder.Configuration.GetSection("StockMonitorConfig"));

            // Registro de serviços (injeção de dependência) e configuração do Worker (responsável pelo monitoramento contínuo)
            builder.Services.AddSingleton(alertParams);
            builder.Services.AddSingleton<IMqttClientWrapper, MqttClientWrapper>();

            builder.Services.AddTransient<IMonitorService, VecnaMonitorService>();
            builder.Services.AddTransient<IMonitorService, B3StockMonitorService>();
            builder.Services.AddTransient<IVecnaProvider, MqttVecnaProvider>();
            builder.Services.AddTransient<IStockProvider, RandomStockProvider>();
            builder.Services.AddTransient<IAlertService, SmtpAlertService>();
            builder.Services.AddTransient<IAlertService, MqttAlertService>();
            builder.Services.AddTransient<IAlertService, ConsoleAlertService>();

            // 4. Registrar o Worker
            builder.Services.AddHostedService<MonitorWorker>();
            builder.Services.AddHostedService<AlertWorker>();

            return builder;
        }
    }
}
