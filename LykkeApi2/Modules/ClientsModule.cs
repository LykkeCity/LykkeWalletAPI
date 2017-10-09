using System;
using Autofac;
using Core.Settings;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.Custom;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Registration;
using Lykke.Service.Wallets.Client.AutorestClient;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

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

            builder.RegisterType<WalletsService>()
                .As<IWalletsService>()
                .WithParameter("baseUri", new Uri(_serviceSettings.CurrentValue.WalletsServiceUrl));
        }
    }
}