using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using FluentValidation.AspNetCore;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using LykkeApi2.Infrastructure;
using LykkeApi2.Infrastructure.Authentication;
using LykkeApi2.Models.ValidationModels;
using LykkeApi2.Modules;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Core.Settings;
using LykkeApi2.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LykkeApi2
{
    public class Startup
    {
        public const string ApiVersion = "v2";
        public const string ApiTitle = "Lykke Wallet API v2";
        public const string ComponentName = "WalletApiV2";

        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; set; }
        public ILog Log { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc(options => { options.Filters.Add<ValidateModelAttribute>(); })
                    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration(ApiVersion, ApiTitle);

                    options.OperationFilter<ApiKeyHeaderOperationFilter>();
                });

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddScheme<LykkeAuthOptions, LykkeAuthHandler>("Bearer", options => { });

                var builder = new ContainerBuilder();
                var appSettings = Configuration.LoadSettings<APIv2Settings>();
                Log = CreateLogWithSlack(services, appSettings);
                builder.Populate(services);
                builder.RegisterModule(new DefaultValueModule());
                builder.RegisterModule(new Api2Module(appSettings, Log));
                builder.RegisterModule(new ClientsModule(appSettings, Log));
                builder.RegisterModule(new AspNetCoreModule());

                ApplicationContainer = builder.Build();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                app.UseLykkeMiddleware(ComponentName, ex => new { Message = "Technical problem" });

                app.UseCors(builder =>
                {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                });
                app.Use(next => context =>
                {
                    context.Request.EnableRewind();

                    return next(context);
                });

                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseAuthentication();

                app.UseMiddleware<ClientBansMiddleware>();
                
                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default-to-swagger",
                        template: "{controller=Swagger}");
                });

                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.UseSwagger();
                app.UseSwaggerUI(o =>
                {
                    o.RoutePrefix = "swagger/ui";
                    o.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiVersion);
                });

                appLifetime.ApplicationStarted.Register(() => StartApplication().Wait());
                appLifetime.ApplicationStopped.Register(() => CleanUp().Wait());
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(Configure), "", ex).Wait();
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Service not yet receive and process requests here

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                // NOTE: Service can't receive and process requests here, so you can destroy all resources

                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
                throw;
            }
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<APIv2Settings> settings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            //Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(
                new Lykke.AzureQueueIntegration.AzureQueueSettings
                {
                    ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
                }, aggregateLogger);

            var dbLogConnectionStringManager = settings.Nested(x => x.WalletApiv2.Db.LogsConnString);
            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            // Creating azure storage logger, which logs own messages to console log
            if (!string.IsNullOrEmpty(dbLogConnectionString) &&
                !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "LogApiv2", consoleLogger),
                    consoleLogger);

                var slackNotificationsManager =
                    new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);

                var azureStorageLogger = new LykkeLogToAzureStorage(
                    persistenceManager,
                    slackNotificationsManager,
                    consoleLogger);

                azureStorageLogger.Start();

                aggregateLogger.AddLog(azureStorageLogger);
            }

            return aggregateLogger;
        }
    }
}
