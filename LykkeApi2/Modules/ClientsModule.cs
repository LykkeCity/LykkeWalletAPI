using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.MarketProfileService.Client;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.Operations.Client;
using Lykke.Service.Registration;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Common.Log;
using Core.Settings;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.ClientDictionaries.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.Session.Client;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.History.Client;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.ClientAccountRecovery.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.SwiftCredentials.Client;
using Lykke.Service.PersonalData;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly ServiceSettings _settings;
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly ILog _log;

        public ClientsModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _apiSettings = settings;
            _settings = settings.Nested(x => x.WalletApiv2.Services).CurrentValue;
            _services = new ServiceCollection();
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterLykkeServiceClient(_settings.ClientAccountServiceUrl);

            builder.RegisterType<HftInternalServiceAPI>()
                .As<IHftInternalServiceAPI>()
                .WithParameter("baseUri", new Uri(_settings.HftInternalServiceUrl));

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_settings.MarketProfileUrl));

            builder.RegisterOperationsClient(_settings.OperationsUrl);

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _settings.RegistrationUrl);

            builder.RegisterClientSessionClient(_apiSettings.CurrentValue.WalletApiv2.Services.SessionUrl, _log);

            builder.RegisterPersonalDataClientAccountRecoveryClient(_apiSettings.CurrentValue.PersonalDataServiceSettings.ServiceUri, _log);

            builder.RegisterInstance(
                new KycStatusServiceClient(_apiSettings.CurrentValue.KycServiceClient, _log))
                .As<IKycStatusService>().SingleInstance();

            builder.RegisterInstance<IAssetDisclaimersClient>(
                new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.AssetsServiceUrl),
                TimeSpan.FromMinutes(60)),_log);


            builder.RegisterClientDictionariesClient(_apiSettings.CurrentValue.ClientDictionariesServiceClient, _log);

            builder.BindMeClient(_apiSettings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), socketLog: null, ignoreErrors: true);

            builder.RegisterFeeCalculatorClient(_apiSettings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl, _log);

            builder.RegisterAffiliateClient(_settings.AffiliateServiceClient.ServiceUrl, _log);

            builder.RegisterInstance<IAssetDisclaimersClient>(new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            builder.RegisterPaymentSystemClient(_apiSettings.CurrentValue.PaymentSystemServiceClient.ServiceUrl, _log);

            builder.RegisterLimitationsServiceClient(_apiSettings.CurrentValue.LimitationServiceClient.ServiceUrl);

            builder.RegisterClientDialogsClient(_apiSettings.CurrentValue.ClientDialogsServiceClient);

            builder.Register(ctx => new BlockchainWalletsClient(_apiSettings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl, _log))
                .As<IBlockchainWalletsClient>()
                .SingleInstance();

            builder.RegisterSwiftCredentialsClient(_apiSettings.CurrentValue.SwiftCredentialsServiceClient);

            builder.RegisterHistoryClient(new HistoryServiceClientSettings { ServiceUrl = _settings.HistoryServiceUrl });

            builder.RegisterConfirmationCodesClient(_apiSettings.Nested(r => r.ConfirmationCodesClient).CurrentValue);
            
            builder.RegisterBlockchainCashoutPreconditionsCheckClient(_apiSettings.CurrentValue.BlockchainCashoutPreconditionsCheckServiceClient.ServiceUrl);

            builder.RegisterClientAccountRecoveryClient(_apiSettings.CurrentValue.ClientRecoveryServiceClient.ServiceUrl,
                _apiSettings.CurrentValue.ClientRecoveryServiceClient.ApiKey);
            
            builder.Populate(_services);
        }
    }
}
