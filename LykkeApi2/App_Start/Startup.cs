using System;
using System.Globalization;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using LykkeApi2.Infrastructure;
using LykkeApi2.Infrastructure.Authentication;
using LykkeApi2.Modules;
using Lykke.SettingsReader;
using IdentityModel;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Lykke.Common;
using Lykke.Cqrs;
using Lykke.Logs;
using LykkeApi2.Infrastructure.LykkeApiError;
using LykkeApi2.Middleware;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace LykkeApi2
{
    public class Startup
    {
        private IReloadingManagerWithConfiguration<APIv2Settings> _appSettings;
        public const string ApiVersion = "v2";
        public const string ApiTitle = "Lykke Wallet API v2";
        public const string ComponentName = "WalletApiV2";

        private IWebHostEnvironment Environment { get; }
        private ILifetimeScope ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _appSettings = Configuration.LoadSettings<APIv2Settings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            });

            services.AddLykkeLogging(_appSettings.Nested(x => x.WalletApiv2.Db.LogsConnString),
                "LogApiv2",
                _appSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                _appSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName);

            services
                .AddControllers(options =>
                {
                    options.Filters.Add(new ProducesAttribute("application/json"));
                })
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                    options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    options.SerializerSettings.Culture = CultureInfo.InvariantCulture;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration(ApiVersion, ApiTitle);

                options.OperationFilter<ObsoleteOperationFilter>();
                options.OperationFilter<SecurityRequirementsOperationFilter>();

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(_appSettings.CurrentValue.SwaggerSettings.Security.AuthorizeEndpoint)
                        }
                    }
                });

                options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    Description = "Old Lykke access token. *It's not required for request, you can use embed 'Authorization' option to use a new one under the hood."
                });

                options.CustomSchemaIds(x => x.FullName);
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
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new Api2Module(_appSettings));
            builder.RegisterModule(new ClientsModule(_appSettings));
            builder.RegisterModule(new AspNetCoreModule());
            builder.RegisterModule(new CqrsModule(_appSettings.CurrentValue));
            builder.RegisterModule(new RepositoriesModule(_appSettings.Nested(x => x.WalletApiv2.Db)));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
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

            app.UseLykkeMiddleware(ex => new { message = "Technical problem" });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseMiddleware<LykkeApiErrorMiddleware>();

            app.Use(next => context =>
            {
                context.Request.EnableBuffering();

                return next(context);
            });

            app.UseMiddleware<CheckSessionMiddleware>();
            app.UseMiddleware<ClientBansMiddleware>();

            app.UseDefaultFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            ApplicationContainer = app.ApplicationServices.GetAutofacRoot();

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

        private Task StartApplication()
        {
            ApplicationContainer.Resolve<ICqrsEngine>().StartSubscribers();
            return Task.CompletedTask;
        }

        private void CleanUp()
        {
            ApplicationContainer.Resolve<MarketDataCacheService>().Stop();
            ApplicationContainer.Dispose();
        }
    }
}
