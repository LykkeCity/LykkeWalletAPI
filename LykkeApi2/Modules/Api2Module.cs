using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories.CashOperations;
using AzureRepositories.Email;
using AzureRepositories.Exchange;
using AzureRepositories.Repositories;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core.Mappers;
using Core.Messages;
using Core.Settings;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.ClientAccount.Client.Custom;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.Registration;
using Lykke.Service.Wallets.Client;
using LykkeApi2.Credentials;
using LykkeApi2.Mappers;
using LykkeApi2.Models.ApiContractModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Lykke.Service.Registration;

namespace LykkeApi2.Modules
{
    public class Api2Module : Module
    {
        private readonly APIv2Settings _settings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;
        private TimeSpan DEFAULT_CACHE_EXPIRATION_PERIOD = TimeSpan.FromHours(1);

        public Api2Module(APIv2Settings settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterInstance<IVerifiedEmailsRepository>(new VerifiedEmailsRepository(
              new AzureTableStorage<VerifiedEmailEntity>(_settings.WalletApiv2.Db.ClientPersonalInfoConnString, "VerifiedEmails", _log)));

            //--------------------------------------------
            builder.RegisterInstance<ILimitTradeEventsRepository>(new LimitTradeEventsRepository(
             new AzureTableStorage<LimitTradeEventEntity>(_settings.WalletApiv2.Db.ClientPersonalInfoConnString, "LimitTradeEvents", _log)));

            builder.RegisterInstance<IMarketOrdersRepository>(new MarketOrdersRepository(
            new AzureTableStorage<MarketOrderEntity>(_settings.WalletApiv2.Db.HMarketOrdersConnString, "MarketOrders", _log)));

            //-------------------------------------------------------
            builder.RegisterOperationsRepositoryClients(_settings.WalletApiv2.Services.OperationsRepositoryClient.ServiceUrl, _log,
                                                        _settings.WalletApiv2.Services.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterInstance<DeploymentSettings>(new DeploymentSettings());
            builder.RegisterInstance(_settings.WalletApiv2.DeploymentSettings);

            _services.UseAssetsClient(AssetServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.AssetsServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD));
            _services.UseClientAccountService(ClientAccountServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.ClientAccountServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD));
            _services.UseClientAccountClient(ClientAccountServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.ClientAccountServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD), _log);

            _services.AddSingleton<ILykkeRegistrationClient>(x => new LykkeRegistrationClient(_settings.WalletApiv2.Services.RegistrationUrl, _log));

            _services.AddSingleton<IWalletsClient>(x => new WalletsClient(_settings.WalletApiv2.Services.WalletsServiceUrl, _log));

            _services.AddSingleton<ClientAccountLogic>();

            _services.AddSingleton<ILykkeMarketProfileServiceAPI>(x => new LykkeMarketProfileServiceAPI(new Uri(_settings.WalletApiv2.Services.MarketProfileUrl)));
            _services.AddSingleton<ICandleshistoryservice>(x => new Candleshistoryservice(new Uri(_settings.WalletApiv2.Services.CandleHistoryUrl)));

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
                   async () => (await ctx.Resolve<ICachedAssetsService>().GetAllAssetsAsync()).ToDictionary(itm => itm.Id));
            }).SingleInstance();

            builder.Register(c =>
            {
                var ctx = c.Resolve<IComponentContext>();
                return new CachedDataDictionary<string, IAssetPair>(
                   async () => (await ctx.Resolve<ICachedAssetsService>().GetAllAssetPairsAsync()).ToDictionary(itm => itm.Id));
            }).SingleInstance();
        }

        private static void BindServices(ContainerBuilder builder, APIv2Settings settings, ILog log)
        {
            builder.RegisterOperationsRepositoryClients(settings.WalletApiv2.Services.OperationsRepositoryClient.ServiceUrl, log,
                                                        settings.WalletApiv2.Services.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterOperationsHistoryClient(settings.WalletApiv2.Services.OperationsHistoryUrl, log);
        }

        private static void BindHistoryMappers(ContainerBuilder builder)
        {
            var historyMapProvider = new HistoryOperationMapProvider();
            var historyMapper = new HistoryOperationMapper<object, ApiBalanceChangeModel, ApiCashOutAttempt, ApiTradeOperation, ApiTransfer>(historyMapProvider);

            builder.RegisterInstance(historyMapper).As<IHistoryOperationMapper<object, HistoryOperationSourceData>>();
        }
    }
}
