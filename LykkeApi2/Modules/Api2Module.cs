using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core.Candles;
using Core.Enumerators;
using Core.Identity;
using Core.Services;
using Core.Settings;
using LkeServices;
using LkeServices.Candles;
using LkeServices.Identity;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using LykkeApi2.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace LykkeApi2.Modules
{
    public class Api2Module : Module
    {
        private readonly ILog _log;
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<BaseSettings> _settings;

        public Api2Module(IReloadingManager<BaseSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterInstance(_settings).SingleInstance();

            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.Services.RateCalculatorServiceApiUrl, _log);

            builder.RegisterBalancesClient(_settings.CurrentValue.Services.BalancesServiceUrl, _log);
            
            builder.RegisterInstance(new DeploymentSettings());

            builder.RegisterInstance(_settings.CurrentValue.DeploymentSettings);

            builder.RegisterInstance<IAssetsService>(
                new AssetsService(new Uri(_settings.CurrentValue.Services.AssetsServiceUrl)));

            builder.RegisterType<ClientAccountLogic>().AsSelf().SingleInstance();
            
            _services.AddSingleton<ICandlesHistoryServiceProvider>(x =>
            {
                var provider = new CandlesHistoryServiceProvider();

                provider.RegisterMarket(MarketType.Spot, _settings.CurrentValue.Services.CandleHistorySpotUrl);
                provider.RegisterMarket(MarketType.Mt, _settings.CurrentValue.Services.CandleHistoryMtUrl);

                return provider;
            });

            builder.RegisterType<RequestContext>().As<IRequestContext>().InstancePerLifetimeScope();

            builder.RegisterType<LykkePrincipal>().As<ILykkePrincipal>().InstancePerLifetimeScope();

            builder.RegisterType<SrvAssetsHelper>().AsSelf().SingleInstance();
            
            builder.RegisterType<HistoryDomainModelConverter>().AsSelf();
            
            RegisterDictionaryEntities(builder);            
            BindServices(builder, _settings, _log);
            builder.Populate(_services);
        }

        private void RegisterDictionaryEntities(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, Asset>(
                    async () =>
                        (await ctx.Resolve<IAssetsService>().AssetGetAllAsync()).ToDictionary(itm => itm.Id));
            }).SingleInstance();
            
            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, AssetPair>(
                    async () =>
                        (await ctx.Resolve<IAssetsService>().AssetPairGetAllAsync())
                        .ToDictionary(itm => itm.Id));
            }).SingleInstance();
        }

        private static void BindServices(ContainerBuilder builder, IReloadingManager<BaseSettings> settings, ILog log)
        {
            var redis = new RedisCache(new RedisCacheOptions
            {
                Configuration = settings.CurrentValue.CacheSettings.RedisConfiguration,
                InstanceName = settings.CurrentValue.CacheSettings.FinanceDataCacheInstance
            });

            builder.RegisterInstance(redis).As<IDistributedCache>().SingleInstance();

            builder.RegisterType<OrderBooksService>()
                .As<IOrderBooksService>()
                .WithParameter(TypedParameter.From(settings.CurrentValue.CacheSettings))
                .SingleInstance();
        }       
    }
}
