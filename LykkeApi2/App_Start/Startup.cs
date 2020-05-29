using System;
using System.Buffers;
using System.Linq;
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
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace LykkeApi2
{
    public class Startup
    {
        private IReloadingManagerWithConfiguration<APIv2Settings> _appSettings;
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
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            _appSettings = Configuration.LoadSettings<APIv2Settings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            });

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });
                services.Configure<MvcOptions>(opts =>
                {
                    var formatter = opts.OutputFormatters.FirstOrDefault(i => i.GetType() == typeof(JsonOutputFormatter));
                    var jsonFormatter = formatter as JsonOutputFormatter;
                    var formatterSettings = jsonFormatter == null
                        ? JsonSerializerSettingsProvider.CreateSerializerSettings()
                        : jsonFormatter.PublicSerializerSettings;
                    if (formatter != null)
                        opts.OutputFormatters.RemoveType<JsonOutputFormatter>();
                    formatterSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    JsonOutputFormatter jsonOutputFormatter = new JsonOutputFormatter(formatterSettings, ArrayPool<char>.Create());
                    opts.OutputFormatters.Insert(0, jsonOutputFormatter);
                });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration(ApiVersion, ApiTitle);

                    options.OperationFilter<ApiKeyHeaderOperationFilter>();
                    options.OperationFilter<ObsoleteOperationFilter>();

                    options.DocumentFilter<SecurityRequirementsDocumentFilter>();

                    options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                    {
                        Type = "oauth2",
                        Flow = "implicit",
                        AuthorizationUrl = _appSettings.CurrentValue.SwaggerSettings.Security.AuthorizeEndpoint
                    });
                });

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
                    context.Request.EnableRewind();

                    return next(context);
                });

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

                app.UseSwagger();
                app.UseSwaggerUI(o =>
                {
                    o.RoutePrefix = "swagger/ui";
                    o.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiVersion);
                    o.OAuthClientId(_appSettings.CurrentValue.SwaggerSettings.Security.OAuthClientId);
                });

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalError(nameof(Startup), nameof(Configure), ex);
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Service not yet receive and process requests here
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
