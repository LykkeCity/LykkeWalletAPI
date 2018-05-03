using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Cache;
using Common.Log;
using Core;
using Core.Candles;
using Core.Countries;
using Core.Enumerators;
using Core.Exchange;
using Core.Identity;
using Core.Services;
using LkeServices;
using LkeServices.Candles;
using LkeServices.Countries;
using LkeServices.Identity;
using LkeServices.Operations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using Core.Settings;
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
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly IReloadingManager<BaseSettings> _settings;

        public Api2Module(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _apiSettings = settings;
            _settings = settings.Nested(x => x.WalletApiv2);
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();
            builder.RegisterType<ClientAccountLogic>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterInstance(_settings.CurrentValue).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.FeeSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.IcoSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.GlobalSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.KycServiceClient).SingleInstance();

            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.Services.RateCalculatorServiceApiUrl, _log);
            builder.RegisterBalancesClient(_settings.CurrentValue.Services.BalancesServiceUrl, _log);

            builder.RegisterInstance(_settings.CurrentValue).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.FeeSettings).SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            builder.RegisterInstance(new DeploymentSettings());
            builder.RegisterInstance(_settings.CurrentValue.DeploymentSettings);
            builder.RegisterInstance<IAssetsService>(
                new AssetsService(new Uri(_settings.CurrentValue.Services.AssetsServiceUrl)));
            builder.RegisterType<SrvDisabledOperations>().SingleInstance();

            _services.AddSingleton<ICandlesHistoryServiceProvider>(x =>
            {
                var provider = new CandlesHistoryServiceProvider();

                provider.RegisterMarket(MarketType.Spot, _settings.CurrentValue.Services.CandleHistorySpotUrl);
                provider.RegisterMarket(MarketType.Mt, _settings.CurrentValue.Services.CandleHistoryMtUrl);

                return provider;
            });

            builder.RegisterType<RequestContext>().As<IRequestContext>().InstancePerLifetimeScope();
            builder.RegisterType<LykkePrincipal>().As<ILykkePrincipal>().InstancePerLifetimeScope();
            //TODO change to v2
            builder.RegisterType<MemoryCacheManager>().As<ICacheManager>();
            builder.RegisterType<CountryPhoneCodeService>().As<ICountryPhoneCodeService>();
            builder.RegisterType<DisableOnMaintenanceFilter>();
            builder.RegisterType<CachedAssetsDictionary>();

            RegisterDictionaryEntities(builder);
            builder.RegisterType<AssetsHelper>().As<IAssetsHelper>().SingleInstance();

            RegisterDictionaryEntities(builder);
            BindServices(builder, _settings);
            builder.Populate(_services);
        }

        private void RegisterDictionaryEntities(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, Asset>(
                    async () =>
                        (await ctx.Resolve<IAssetsService>().AssetGetAllAsync(true))
                        .ToDictionary(itm => itm.Id),
                    60);
            }).SingleInstance();

            builder.Register(x =>
            {
                var assetsService = x.Resolve<IComponentContext>().Resolve<IAssetsService>();

                return new CachedAssetsDictionary
                (
                    async () => (await assetsService.AssetGetAllAsync(includeNonTradable: true)).ToDictionary(itm => itm.Id)
                );
            }).SingleInstance();

            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, AssetPair>(
                    async () =>
                        (await ctx.Resolve<IAssetsService>().AssetPairGetAllAsync())
                        .ToDictionary(itm => itm.Id),
                    60);
            }).SingleInstance();

            builder.Register(x =>
            {
                var ctx = x.Resolve<IComponentContext>();

                return new CachedTradableAssetsDictionary
                (
                    async () =>
                        (await ctx.Resolve<IAssetsService>().AssetGetAllAsync())
                        .ToDictionary(itm => itm.Id)
                );
            }).SingleInstance();
        }

        private static void BindServices(ContainerBuilder builder, IReloadingManager<BaseSettings> settings)
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