using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common;
using Common.Log;
using Core.Identity;
using Core.Mappers;
using Core.Settings;
using LkeServices.Identity;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using LykkeApi2.Mappers;
using LykkeApi2.Models.ApiContractModels;
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
            builder.RegisterInstance(_settings).SingleInstance();

            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.Services.RateCalculatorServiceApiUrl, _log);

            builder.RegisterBalancesClient(_settings.CurrentValue.Services.BalancesServiceUrl, _log);

            builder.RegisterInstance(new DeploymentSettings());

            builder.RegisterInstance(_settings.CurrentValue.DeploymentSettings);

            builder.RegisterInstance<IAssetsService>(
                new AssetsService(new Uri(_settings.CurrentValue.Services.AssetsServiceUrl)));

            _services.AddSingleton<ClientAccountLogic>();

            builder.RegisterType<RequestContext>().As<IRequestContext>().InstancePerLifetimeScope();

            builder.RegisterType<LykkePrincipal>().As<ILykkePrincipal>().InstancePerLifetimeScope();

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