using System;
using Autofac;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.Registration;
using Lykke.SettingsReader;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Core.Blockchain;
using LkeServices.Blockchain;
using Lykke.Common.Log;
using Lykke.Exchange.Api.MarketData.Contract;
using Lykke.HttpClientGenerator.Caching;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Payments.Link4Pay.Contract;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.ClientDictionaries.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.Session.Client;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainSettings.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.History.Client;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.IndicesFacade.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PushNotifications.Client;
using Lykke.Service.SwiftCredentials.Client;
using Lykke.Service.Tier.Client;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly ServiceSettings _settings;
        private readonly BlockchainSettingsServiceClientSettings _blockchainSettingsServiceClient;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;

        public ClientsModule(IReloadingManager<APIv2Settings> settings)
        {
            _apiSettings = settings;
            _settings = settings.Nested(x => x.WalletApiv2.Services).CurrentValue;
            _blockchainSettingsServiceClient = settings.Nested(x => x.BlockchainSettingsServiceClient).CurrentValue;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterClientAccountClient(_settings.ClientAccountServiceUrl);

            builder.RegisterHftInternalClient(_settings.HftInternalServiceUrl);

            builder.RegisterType<LykkeMarketProfileServiceAPI>()
                .As<ILykkeMarketProfileServiceAPI>()
                .WithParameter("baseUri", new Uri(_settings.MarketProfileUrl));

            builder.RegisterOperationsClient(_settings.OperationsUrl);

            builder.Register(ctx =>
                    new LykkeRegistrationClient(_settings.RegistrationUrl,
                        ctx.Resolve<ILogFactory>().CreateLog(nameof(LykkeRegistrationClient))))
                .As<ILykkeRegistrationClient>().SingleInstance();

            builder.RegisterClientSessionClient(new SessionServiceClientSettings{ ServiceUrl = _apiSettings.CurrentValue.WalletApiv2.Services.SessionUrl });

            builder.RegisterType<PersonalDataService>().As<IPersonalDataService>()
                .WithParameter(TypedParameter.From(_apiSettings.CurrentValue.PersonalDataServiceSettings));

            builder.Register(ctx =>
                new KycStatusServiceClient(_apiSettings.CurrentValue.KycServiceClient, ctx.Resolve<ILogFactory>()))
                .As<IKycStatusService>()
                .SingleInstance();

            builder.Register(ctx =>
                new KycProfileServiceClient(_apiSettings.CurrentValue.KycServiceClient, ctx.Resolve<ILogFactory>()))
                .As<IKycProfileService>()
                .SingleInstance();

            builder.RegisterInstance<IAssetDisclaimersClient>(
                new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            builder.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.AssetsServiceUrl),
                TimeSpan.FromMinutes(60)));

            var logFactory = LogFactory.Create().AddConsole();
            builder.RegisterClientDictionariesClient(_apiSettings.CurrentValue.ClientDictionariesServiceClient, logFactory.CreateLog(nameof(ClientDictionariesClient)));

            builder.RegisterMeClient(_apiSettings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), true, ignoreErrors: true);

            builder.RegisterFeeCalculatorClient(_apiSettings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl);

            builder.RegisterAffiliateClient(_settings.AffiliateServiceClient.ServiceUrl, logFactory.CreateLog(nameof(AffiliateClient)));

            builder.RegisterIndicesFacadeClient(new IndicesFacadeServiceClientSettings { ServiceUrl = _settings.IndicesFacadeServiceUrl }, null);

            builder.RegisterInstance<IAssetDisclaimersClient>(new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            builder.RegisterPaymentSystemClient(_apiSettings.CurrentValue.PaymentSystemServiceClient.ServiceUrl, logFactory.CreateLog(nameof(PaymentSystemClient)));

            builder.RegisterLimitationsServiceClient(_apiSettings.CurrentValue.LimitationServiceClient.ServiceUrl);

            builder.RegisterClientDialogsClient(_apiSettings.CurrentValue.ClientDialogsServiceClient);

            builder.Register(ctx => new BlockchainWalletsClient(_apiSettings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl, ctx.Resolve<ILogFactory>(), new BlockchainWalletsApiFactory()))
                .As<IBlockchainWalletsClient>()
                .SingleInstance();

            builder.RegisterSwiftCredentialsClient(_apiSettings.CurrentValue.SwiftCredentialsServiceClient);

            builder.RegisterHistoryClient(new HistoryServiceClientSettings { ServiceUrl = _settings.HistoryServiceUrl });

            builder.RegisterConfirmationCodesClient(_apiSettings.Nested(r => r.ConfirmationCodesClient).CurrentValue);

            builder.RegisterBlockchainCashoutPreconditionsCheckClient(_apiSettings.CurrentValue.BlockchainCashoutPreconditionsCheckServiceClient.ServiceUrl);

            #region BlockchainSettings

            var settings = _blockchainSettingsServiceClient;

            var cacheManager = new ClientCacheManager();
            var factory =
                new Lykke.Service.BlockchainSettings.Client.HttpClientGenerator.BlockchainSettingsClientFactory();
            var client = factory.CreateNew(settings.ServiceUrl, settings.ApiKey, true, cacheManager);
            builder.RegisterInstance(client)
                .As<IBlockchainSettingsClient>();
            builder.RegisterInstance(client)
                .As<IBlockchainSettingsClient>()
                .SingleInstance();

            builder.RegisterInstance(cacheManager)
                .As<IClientCacheManager>()
                .SingleInstance();

            builder.RegisterType<BlockchainExplorersProvider>()
                .As<IBlockchainExplorersProvider>()
                .SingleInstance();

            #endregion

            builder.RegisterPushNotificationsClient(_apiSettings.CurrentValue.PushNotificationsServiceClient.ServiceUrl);

            builder.RegisterTierClient(_apiSettings.CurrentValue.TierServiceClient);

            builder.RegisterMarketDataClient(_apiSettings.CurrentValue.MarketDataServiceClient);
            builder.RegisterLink4PayClient(_apiSettings.CurrentValue.Link4PayServiceClient);
        }
    }
}
