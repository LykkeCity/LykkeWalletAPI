using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Core.Settings;
using FluentValidation.AspNetCore;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.SettingsReader;
using LykkeApi2.Infrastructure;
using LykkeApi2.Infrastructure.Authentication;
using LykkeApi2.Models.ValidationModels;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SQLitePCL;

namespace LykkeApi2
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
            services.AddMvc(options =>
                {
                    options.Filters.Add<ValidateModelAttribute>();
                })                
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())                
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });
            
            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration(apiVersion, appName);

                options.OperationFilter<ApiKeyHeaderOperationFilter>();
            });            

            var builder = new ContainerBuilder();
            var apiSettings = Configuration.LoadSettings<APIv2Settings>();

            var log = CreateLogWithSlack(services, apiSettings.CurrentValue.WalletApiv2, apiSettings.ConnectionString(x => x.WalletApiv2.Db.LogsConnString));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.LoginPath = new PathString("/login");
                })
                .AddOpenIdConnect(options =>
                {
                    options.Authority = apiSettings.CurrentValue.WalletApiv2.Authentication.Authority;
                    options.ClientId = apiSettings.CurrentValue.WalletApiv2.Authentication.ClientId;
                    options.ClientSecret = apiSettings.CurrentValue.WalletApiv2.Authentication.ClientSecret;
                    options.RequireHttpsMetadata = true;
                    options.SaveTokens = true;
                    options.CallbackPath = "/auth";
                    options.ResponseType = OpenIdConnectResponseType.Code;                    
                });

            builder.RegisterModule(new Api2Module(apiSettings.Nested(x => x.WalletApiv2), log));
            builder.RegisterModule(new ClientsModule(apiSettings.Nested(x => x.WalletApiv2.Services)));
            builder.RegisterModule(new AspNetCoreModule());
            builder.Populate(services);
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
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
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                name: "default-to-swagger",
                template: "{controller=Swagger}");
            });
            
            CreateErrorResponse responseFactory = exception => exception;
            app.UseMiddleware<GlobalErrorHandlerMiddleware>("WalletApiV2", responseFactory);
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
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

        private static ILog CreateLogWithSlack(IServiceCollection services, BaseSettings apiSettings, IReloadingManager<string> dbLogConnectionStringManager)
        {            
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            //var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueSettings
            //{
            //    ConnectionString = settings.WalletApi.SlackIntegration..AzureQueue.ConnectionString,
            //    QueueName = settings.SlackNotifications.AzureQueue.QueueName
            //}, aggregateLogger);

            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(                    
                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "LogApiv2", consoleLogger),
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
