using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Cache;
using Core.Candles;
using Core.Countries;
using Core.Enumerators;
using Core.Identity;
using Core.Services;
using LkeServices;
using LkeServices.Blockchain;
using LkeServices.Candles;
using LkeServices.Countries;
using LkeServices.Identity;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using LykkeApi2.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LykkeApi2.Modules
{
    public class Api2Module : Module
    {
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly BaseSettings _settings;

        public Api2Module(IReloadingManager<APIv2Settings> settings)
        {
            _apiSettings = settings;
            _settings = settings.Nested(x => x.WalletApiv2).CurrentValue;
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

            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.FeeSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.IcoSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.GlobalSettings).SingleInstance();
            builder.RegisterInstance(_apiSettings.CurrentValue.KycServiceClient).SingleInstance();

            builder.RegisterRateCalculatorClient(_settings.Services.RateCalculatorServiceApiUrl);
            builder.RegisterBalancesClient(_settings.Services.BalancesServiceUrl);

            builder.RegisterInstance(new DeploymentSettings());
            builder.RegisterInstance(_settings.DeploymentSettings);
            builder.RegisterInstance<IAssetsService>(
                new AssetsService(new Uri(_settings.Services.AssetsServiceUrl)));

            _services.AddSingleton<ClientAccountLogic>();

            _services.AddSingleton<ICandlesHistoryServiceProvider>(x =>
            {
                var provider = new CandlesHistoryServiceProvider();

                provider.RegisterMarket(MarketType.Spot, _settings.Services.CandleHistorySpotUrl);
                if (!_settings.IsMtDisabled.HasValue || !_settings.IsMtDisabled.Value)
                    provider.RegisterMarket(MarketType.Mt, _settings.Services.CandleHistoryMtUrl);

                return provider;
            });

            builder.RegisterType<RequestContext>().As<IRequestContext>().InstancePerLifetimeScope();
            builder.RegisterType<LykkePrincipal>().As<ILykkePrincipal>().InstancePerLifetimeScope();
            //TODO change to v2
            builder.RegisterType<MemoryCacheManager>().As<ICacheManager>();
            builder.RegisterType<CountryPhoneCodeService>().As<ICountryPhoneCodeService>();

            builder.RegisterType<AssetsHelper>().As<IAssetsHelper>().SingleInstance();

            builder.RegisterType<SrvBlockchainHelper>().As<ISrvBlockchainHelper>().SingleInstance();
            builder.RegisterType<Google2FaService>().SingleInstance();

            BindServices(builder, _settings);

            builder.Populate(_services);
        }

        private void BindServices(ContainerBuilder builder, BaseSettings settings)
        {
            _services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = settings.CacheSettings.RedisConfiguration;
                options.InstanceName = settings.CacheSettings.FinanceDataCacheInstance;
            });

            builder.RegisterType<OrderBooksService>()
                .As<IOrderBooksService>()
                .WithParameter(TypedParameter.From(settings.CacheSettings))
                .SingleInstance();

            builder.RegisterType<KycStatusValidator>()
                .AsSelf();

            builder.RegisterType<KycCountryValidator>()
                .AsSelf();

            builder.RegisterType<MarketDataCacheService>()
                .As<IStartable>()
                .AsSelf()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterInstance(settings.SessionCheck);
        }
    }
}
