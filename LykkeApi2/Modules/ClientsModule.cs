using System;
using Autofac;
using Core.Settings;
using Lykke.MarketProfileService.Client;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Operations.Client;
using Lykke.Service.Registration;
using Lykke.Service.Session;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<ServiceSettings> _serviceSettings;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;

        public ClientsModule(IReloadingManager<APIv2Settings> settings)
        {
            _apiSettings = settings;
            _serviceSettings = settings.Nested(x => x.WalletApiv2.Services);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterLykkeServiceClient(_serviceSettings.CurrentValue.ClientAccountServiceUrl);

            builder.RegisterType<HftInternalServiceAPI>()
                .As<IHftInternalServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.HftInternalServiceUrl));

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.MarketProfileUrl));
            
            builder.RegisterOperationsClient(_serviceSettings.CurrentValue.OperationsUrl);

            builder.RegisterType<Candleshistoryservice>()
                .As<ICandleshistoryservice>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.CandleHistoryUrl));

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.RegistrationUrl);

            builder.RegisterType<ClientSessionsClient>()
                .As<IClientSessionsClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.SessionUrl);

            builder.RegisterType<PersonalDataService>().As<IPersonalDataService>()
                .WithParameter(TypedParameter.From(_apiSettings.CurrentValue.PersonalDataServiceSettings));
        }
    }
}