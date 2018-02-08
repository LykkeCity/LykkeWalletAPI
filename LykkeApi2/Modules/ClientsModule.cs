using System;
using Autofac;
using Lykke.MarketProfileService.Client;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Operations.Client;
using Lykke.Service.Registration;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Common.Log;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.Session.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Settings;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<ServiceSettings> _serviceSettings;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly ILog _log;

        public ClientsModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _apiSettings = settings;
            _serviceSettings = settings.Nested(x => x.WalletApiv2.Services);
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PhoneSignFilter>();

            builder.RegisterLykkeServiceClient(_serviceSettings.CurrentValue.ClientAccountServiceUrl);
            builder.RegisterHftInternalServiceClient(_serviceSettings.CurrentValue.HftInternalServiceUrl, _log);

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.MarketProfileUrl));
            
            builder.RegisterOperationsClient(_serviceSettings.CurrentValue.OperationsUrl);

            builder.RegisterType<Candleshistoryservice>()
                .As<ICandleshistoryservice>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.CandleHistorySpotUrl));

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.RegistrationUrl);

            builder.RegisterRedisClientSession(_apiSettings.Nested(s => s.SessionsSettings).CurrentValue);
            
            builder.RegisterType<PersonalDataService>().As<IPersonalDataService>()
                .WithParameter(TypedParameter.From(_apiSettings.CurrentValue.PersonalDataServiceSettings));

            builder.RegisterOperationsHistoryClient(_apiSettings.CurrentValue.OperationsHistoryServiceClient, _log);
        }
    }
}