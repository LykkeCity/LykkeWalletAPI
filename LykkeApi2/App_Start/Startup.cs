﻿using System;
using System.Buffers;
using Antares.Service.MarketProfile.Client;
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
using LykkeApi2.Modules;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using IdentityModel;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Lykke.Common;
using Lykke.Cqrs;
using LykkeApi2.Infrastructure.LykkeApiError;
using LykkeApi2.Middleware;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Prometheus;
using Swisschain.Sdk.Metrics.Rest;

namespace LykkeApi2
{
    public class Startup
    {
        private readonly IReloadingManagerWithConfiguration<APIv2Settings> _appSettings;
        private const string ApiVersion = "v2";
        private const string ApiTitle = "Lykke Wallet API v2";
        public const string ComponentName = "WalletApiV2";

        private IContainer ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; set; }
        private ILog Log { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            _appSettings = Configuration.LoadSettings<APIv2Settings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            });
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    });

                services.Configure<MvcOptions>(options =>
                {
                    options.EnableEndpointRouting = false;
                });

                services.Configure<MvcOptions>(opts =>
                {
                    var serializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
                    serializerSettings.ContractResolver = new DefaultContractResolver();
                    serializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    var jsonOutputFormatter = new NewtonsoftJsonOutputFormatter(serializerSettings, ArrayPool<char>.Create(), opts);
                    opts.OutputFormatters.Insert(0, jsonOutputFormatter);
                });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration(ApiVersion, ApiTitle);

                    options.OperationFilter<ObsoleteOperationFilter>();
                    options.OperationFilter<SecurityRequirementsOperationFilter>();

                    options.CustomSchemaIds(type => type.ToString());

                    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer",
                        BearerFormat = "JWT"
                    });
                });

                services.AddSwaggerGenNewtonsoftSupport();

                services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
                    .AddOAuth2Introspection(options =>
                    {
                        options.Authority = _appSettings.CurrentValue.WalletApiv2.OAuthSettings.Authority;
                        options.ClientId = _appSettings.CurrentValue.WalletApiv2.OAuthSettings.ClientId;
                        options.ClientSecret = _appSettings.CurrentValue.WalletApiv2.OAuthSettings.ClientSecret;
                        options.NameClaimType = JwtClaimTypes.Subject;
                        options.EnableCaching = true;
                        options.CacheDuration = TimeSpan.FromMinutes(1);
                        options.SkipTokensWithDots = true;
                    }).CustomizeServerAuthentication();

                services.Configure<ApiBehaviorOptions>(options =>
                    {
                        // Wrap failed model state into LykkeApiErrorResponse.
                        options.InvalidModelStateResponseFactory =
                            InvalidModelStateResponseFactory.CreateInvalidModelResponse;
                    });

                var builder = new ContainerBuilder();
                Log = CreateLogWithSlack(services, _appSettings);
                builder.Populate(services);
                builder.RegisterModule(new Api2Module(_appSettings, Log));
                builder.RegisterModule(new ClientsModule(_appSettings, Log));
                builder.RegisterModule(new AspNetCoreModule());
                builder.RegisterModule(new CqrsModule(_appSettings.CurrentValue, Log));
                builder.RegisterModule(new RepositoriesModule(_appSettings.Nested(x => x.WalletApiv2.Db), Log));

                ApplicationContainer = builder.Build();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalError(nameof(Startup), nameof(ConfigureServices), ex);
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                app.Use(async (context, next) =>
                {
                    if (context.Request.Method == "OPTIONS")
                    {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("");
                    }
                    else
                    {
                        await next.Invoke();
                    }
                });

                app.UseLykkeMiddleware(ComponentName, ex => new { message = "Technical problem" });

                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseMiddleware<LykkeApiErrorMiddleware>();

                app.Use(next => context =>
                {
                    context.Request.EnableBuffering();

                    return next(context);
                });

                app.UseAuthentication();

                app.UseMetricServer();

                app.UseMiddleware<PrometheusMetricsMiddleware>();

                app.UseMiddleware<CheckSessionMiddleware>();
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

                app.UseSwagger();
                app.UseSwaggerUI(o =>
                {
                    o.RoutePrefix = "swagger/ui";
                    o.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiVersion);
                    o.OAuthClientId(_appSettings.CurrentValue.SwaggerSettings.Security.OAuthClientId);
                });

                appLifetime.ApplicationStarted.Register(StartApplication);
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalError(nameof(Startup), nameof(Configure), ex);
                throw;
            }
        }

        private void StartApplication()
        {
            try
            {
                // NOTE: Service not yet receive and process requests here
                var marketProfile = ApplicationContainer.Resolve<IMarketProfileServiceClient>();
                marketProfile.Start();
                ApplicationContainer.Resolve<ICqrsEngine>().StartSubscribers();

                Log.WriteMonitor("", AppEnvironment.EnvInfo, "Started");
            }
            catch (Exception ex)
            {
                Log.WriteFatalError(nameof(Startup), nameof(StartApplication), ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Service can't receive and process requests here, so you can destroy all resources
                ApplicationContainer.Resolve<MarketDataCacheService>().Stop();
                Log?.WriteMonitor("", AppEnvironment.EnvInfo, "Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    Log.WriteFatalError(nameof(Startup), nameof(CleanUp), ex);
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
