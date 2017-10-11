using System;
using Autofac;
using Core.Settings;
using Lykke.MarketProfileService.Client;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Registration;
using Lykke.Service.Session;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<ServiceSettings> _serviceSettings;

        public ClientsModule(IReloadingManager<ServiceSettings> serviceSettings)
        {
            _serviceSettings = serviceSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.ClientAccountServiceUrl));

            builder.RegisterType<HftInternalServiceAPI>()
                .As<IHftInternalServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.HftInternalServiceUrl));

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.MarketProfileUrl));

            builder.RegisterType<Candleshistoryservice>()
                .As<ICandleshistoryservice>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.CandleHistoryUrl));

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.RegistrationUrl);

            builder.RegisterType<ClientSessionsClient>()
                .As<IClientSessionsClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.SessionUrl);
        }
    }
}