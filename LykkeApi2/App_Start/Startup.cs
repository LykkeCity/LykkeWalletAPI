using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Core.Settings;
using FluentValidation.AspNetCore;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Service.Assets.Client.Custom;
using Lykke.SettingsReader;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;


namespace LykkeApi2.App_Start
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public IConfigurationRoot Configuration { get; set; }
        public APIv2Settings settingsLocal { get; set; }
        public const string apiVersion = "v2";
        public const string appName = "Lykke Wallet API v2";


        public Startup(IHostingEnvironment env, APIv2Settings settings = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Environment = env;

            settingsLocal = settings;

            Console.WriteLine($"ENV_INFO: {System.Environment.GetEnvironmentVariable("ENV_INFO")}");
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                .AddFluentValidation()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });


            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration(apiVersion, appName);
            });


            var builder = new ContainerBuilder();
            var apiSettings = settingsLocal ?? (Environment.IsDevelopment()
                ? Configuration.Get<APIv2Settings>()
                : HttpSettingsLoader.Load<APIv2Settings>(Configuration.GetValue<string>("apiv2SettingsUrl")));

            var log = CreateLogWithSlack(services, apiSettings);

            builder.RegisterModule(new Api2Module(apiSettings, log));
            builder.Populate(services);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware(appName, ex => new { Message = "Technical problem" });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                name: "default-to-swagger",
                template: "{controller=Swagger}");
            });

            app.UseSwagger();
            app.UseSwaggerUi(swaggerUrl: $"/swagger/{apiVersion}/swagger.json");
            app.UseStaticFiles();

            appLifetime.ApplicationStopping.Register(StopApplication);
            appLifetime.ApplicationStopped.Register(CleanUp);
        }

        private void StopApplication() { }

        private void CleanUp()
        {
            ApplicationContainer.Dispose();
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, APIv2Settings settings)
        {
            var apiSettings = settings.WalletApiv2;
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            //var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueSettings
            //{
            //    ConnectionString = settings.WalletApi.SlackIntegration..AzureQueue.ConnectionString,
            //    QueueName = settings.SlackNotifications.AzureQueue.QueueName
            //}, aggregateLogger);

            var dbLogConnectionString = apiSettings.Db.LogsConnString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    appName,
                    AzureTableStorage<LogEntity>.Create(() => dbLogConnectionString, "LogApiv2", consoleLogger),
                    consoleLogger);

                //var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(appName, slackService, consoleLogger);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    appName,
                    persistenceManager,
                    null,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            return aggregateLogger;
        }
    }
}
