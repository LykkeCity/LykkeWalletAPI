using System;
using Antares.Service.MarketProfile.Client;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Core.Blockchain;
using LkeServices.Blockchain;
using Lykke.Common.Log;
using Lykke.Exchange.Api.MarketData.Contract;
using Lykke.HttpClientGenerator.Caching;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Payments.Link4Pay.Contract;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainSettings.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDictionaries.Client;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.History.Client;
using Lykke.Service.IndicesFacade.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PushNotifications.Client;
using Lykke.Service.Registration;
using Lykke.Service.Session.Client;
using Lykke.Service.SwiftCredentials.Client;
using Lykke.Service.Tier.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace LykkeApi2.Modules
{
    public class ClientsModule : Module
    {
        private readonly ServiceSettings _settings;
        private readonly BlockchainSettingsServiceClientSettings _blockchainSettingsServiceClient;
        private readonly IServiceCollection _services;
        private readonly IReloadingManager<APIv2Settings> _apiSettings;
        private readonly ILog _log;

        public ClientsModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _apiSettings = settings;
            _settings = settings.Nested(x => x.WalletApiv2.Services).CurrentValue;
            _blockchainSettingsServiceClient = settings.Nested(x => x.BlockchainSettingsServiceClient).CurrentValue;
            _services = new ServiceCollection();
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterClientAccountClient(_settings.ClientAccountServiceUrl);

            builder.RegisterHftInternalClient(_settings.HftInternalServiceUrl);

            builder.Register((x) =>
                {
                    var marketProfile = new MarketProfileServiceClient(
                        _settings.MyNoSqlServer.ReaderServiceUrl, 
                        _settings.MarketProfileUrl);
                    marketProfile.Start();

                    return marketProfile;
                })
                .As<IMarketProfileServiceClient>()
                .SingleInstance()
                .AutoActivate();

            builder.RegisterOperationsClient(_settings.OperationsUrl);

            builder.RegisterType<LykkeRegistrationClient>()
                .As<ILykkeRegistrationClient>()
                .WithParameter("serviceUrl", _settings.RegistrationUrl);

            builder.RegisterClientSessionClient(new SessionServiceClientSettings{ ServiceUrl = _apiSettings.CurrentValue.WalletApiv2.Services.SessionUrl });

            builder.RegisterType<PersonalDataService>().As<IPersonalDataService>()
                .WithParameter(TypedParameter.From(_apiSettings.CurrentValue.PersonalDataServiceSettings));

            builder.RegisterInstance(
                new KycStatusServiceClient(_apiSettings.CurrentValue.KycServiceClient, _log))
                .As<IKycStatusService>()
                .SingleInstance();

            builder.RegisterInstance(
                new KycProfileServiceClient(_apiSettings.CurrentValue.KycServiceClient, _log))
                .As<IKycProfileService>()
                .SingleInstance();

            builder.RegisterInstance<IAssetDisclaimersClient>(
                new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.AssetsServiceUrl),
                TimeSpan.FromMinutes(60)),_log);


            builder.RegisterClientDictionariesClient(_apiSettings.CurrentValue.ClientDictionariesServiceClient, _log);

            var socketLog = new SocketLogDynamic(
                i => { },
                s => _log.WriteInfo("MeClient", null, s));
            builder.BindMeClient(_apiSettings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), socketLog, ignoreErrors: true);

            builder.RegisterFeeCalculatorClient(_apiSettings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl, _log);

            builder.RegisterAffiliateClient(_settings.AffiliateServiceClient.ServiceUrl, _log);

            builder.RegisterIndicesFacadeClient(new IndicesFacadeServiceClientSettings { ServiceUrl = _settings.IndicesFacadeServiceUrl }, null);

            builder.RegisterInstance<IAssetDisclaimersClient>(new AssetDisclaimersClient(_apiSettings.CurrentValue.AssetDisclaimersServiceClient));

            builder.RegisterPaymentSystemClient(_apiSettings.CurrentValue.PaymentSystemServiceClient.ServiceUrl, _log);

            builder.RegisterLimitationsServiceClient(_apiSettings.CurrentValue.LimitationServiceClient.ServiceUrl);

            builder.RegisterClientDialogsClient(_apiSettings.CurrentValue.ClientDialogsServiceClient);

            builder.Register(ctx => new BlockchainWalletsClient(_apiSettings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl, _log, new BlockchainWalletsApiFactory()))
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

            builder.RegisterInstance(
                new Swisschain.Sirius.Api.ApiClient.ApiClient(_apiSettings.CurrentValue.SiriusApiServiceClient.GrpcServiceUrl, _apiSettings.CurrentValue.SiriusApiServiceClient.ApiKey)
            ).As<Swisschain.Sirius.Api.ApiClient.IApiClient>();

            builder.Populate(_services);
        }
    }
}
