using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common;
using Common.Log;
using Core.Mappers;
using Core.Settings;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.Registration;
using Lykke.Service.Balances.Client;
using LykkeApi2.Credentials;
using LykkeApi2.Mappers;
using LykkeApi2.Models.ApiContractModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using Lykke.Service.RateCalculator.Client;
using System.Linq;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class Api2Module : Module
    {
        private readonly IReloadingManager<BaseSettings> _settings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;
        private TimeSpan DEFAULT_CACHE_EXPIRATION_PERIOD = TimeSpan.FromHours(1);

        public Api2Module(IReloadingManager<BaseSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();

            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterOperationsRepositoryClients(
                _settings.CurrentValue.Services.OperationsRepositoryClient.ServiceUrl, _log,
                _settings.CurrentValue.Services.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterOperationsRepositoryClients(
                _settings.CurrentValue.Services.OperationsRepositoryClient.ServiceUrl, _log,
                _settings.CurrentValue.Services.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.Services.RateCalculatorServiceApiUrl, _log);

            builder.RegisterBalancesClient(_settings.CurrentValue.Services.BalancesServiceUrl, _log);

            builder.RegisterInstance(new DeploymentSettings());

            builder.RegisterInstance(_settings.CurrentValue.DeploymentSettings);

            _services.UseAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Services.AssetsServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD));

            _services.AddSingleton<ILykkeRegistrationClient>(x =>
                new LykkeRegistrationClient(_settings.CurrentValue.Services.RegistrationUrl, _log));

            _services.AddSingleton<ClientAccountLogic>();

            _services.AddSingleton<ILykkeMarketProfileServiceAPI>(x =>
                new LykkeMarketProfileServiceAPI(new Uri(_settings.CurrentValue.Services.MarketProfileUrl)));

            _services.AddSingleton<ICandleshistoryservice>(x =>
                new Candleshistoryservice(new Uri(_settings.CurrentValue.Services.CandleHistoryUrl)));

            RegisterDictionaryEntities(builder);
            BindHistoryMappers(builder);
            BindServices(builder, _settings, _log);
            builder.Populate(_services);
        }

        private void RegisterDictionaryEntities(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAsset>(
                    async () =>
                        (await ctx.Resolve<ICachedAssetsService>().GetAllAssetsAsync()).ToDictionary(itm => itm.Id));
            }).SingleInstance();

            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAssetPair>(
                    async () =>
                        (await ctx.Resolve<ICachedAssetsService>().GetAllAssetPairsAsync())
                        .ToDictionary(itm => itm.Id));
            }).SingleInstance();
        }

        private static void BindServices(ContainerBuilder builder, IReloadingManager<BaseSettings> settings, ILog log)
        {
            builder.RegisterOperationsRepositoryClients(
                settings.CurrentValue.Services.OperationsRepositoryClient.ServiceUrl, log,
                settings.CurrentValue.Services.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterOperationsHistoryClient(settings.CurrentValue.Services.OperationsHistoryUrl, log);
        }

        private static void BindHistoryMappers(ContainerBuilder builder)
        {
            var historyMapProvider = new HistoryOperationMapProvider();
            var historyMapper =
                new HistoryOperationMapper<object, ApiBalanceChangeModel, ApiCashOutAttempt, ApiTradeOperation,
                    ApiTransfer>(historyMapProvider);

            builder.RegisterInstance(historyMapper).As<IHistoryOperationMapper<object, HistoryOperationSourceData>>();
        }
    }
}