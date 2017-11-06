﻿using System;
using Autofac;
using Core.Settings;
using Lykke.MarketProfileService.Client;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Operations.Client.AutorestClient;
using Lykke.Service.Registration;
using Lykke.Service.Session;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Pledges.Client;

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
            builder.RegisterLykkeServiceClient(_serviceSettings.CurrentValue.ClientAccountServiceUrl);

            builder.RegisterType<HftInternalServiceAPI>()
                .As<IHftInternalServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.HftInternalServiceUrl));

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.MarketProfileUrl));

            builder.RegisterType<OperationsAPI>()
                .As<IOperationsAPI>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.OperationsUrl));

            builder.RegisterType<Candleshistoryservice>()
                .As<ICandleshistoryservice>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.CandleHistoryUrl));

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.RegistrationUrl);

            builder.RegisterType<ClientSessionsClient>()
                .As<IClientSessionsClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.SessionUrl);

            builder.RegisterType<PledgesClient>()
                .As<IPledgesClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.PledgesServiceUrl);
        }
    }
}