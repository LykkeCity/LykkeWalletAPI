using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.Registration;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.OperationsRepository.Client;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.Session.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Settings;
using Lykke.Service.Affiliate.Client;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<ServiceSettings> _serviceSettings;
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly ILog _log;

        public ClientsModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _apiSettings = settings;
            _serviceSettings = settings.Nested(x => x.WalletApiv2.Services);
            _services = new ServiceCollection();
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
            
            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _serviceSettings.CurrentValue.RegistrationUrl);

            builder.RegisterClientSessionClient(_apiSettings.Nested(s => s.WalletApiv2.Services.SessionUrl).CurrentValue, _log);

            builder.RegisterType<PersonalDataService>().As<IPersonalDataService>()
                .WithParameter(TypedParameter.From(_apiSettings.CurrentValue.PersonalDataServiceSettings));

            builder.RegisterOperationsHistoryClient(_apiSettings.CurrentValue.OperationsHistoryServiceClient, _log);
            
            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_serviceSettings.CurrentValue.AssetsServiceUrl),
                TimeSpan.FromMinutes(1)));
            
            builder.BindMeClient(_apiSettings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), socketLog: null, ignoreErrors: true);
            
            builder.RegisterOperationsRepositoryClients(_serviceSettings.CurrentValue.OperationsRepositoryClient, _log);
            
            builder.RegisterAffiliateClient(_serviceSettings.CurrentValue.AffiliateServiceClient.ServiceUrl, _log);

            builder.RegisterFeeCalculatorClient(_apiSettings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl, _log);
            

            builder.Populate(_services);
        }
    }
}
