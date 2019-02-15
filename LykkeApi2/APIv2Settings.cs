using System.Net;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDictionaries.Client;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.SwiftCredentials.Client;
using Lykke.SettingsReader.Attributes;

namespace LykkeApi2
{
    public class APIv2Settings
    {
        public BaseSettings WalletApiv2 { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public PersonalDataServiceClientSettings PersonalDataServiceSettings { get; set; }
        public BlockchainWalletsServiceClientSettings BlockchainWalletsServiceClient { get; set; }
        public BlockchainCashoutPreconditionsCheckServiceClientSettings BlockchainCashoutPreconditionsCheckServiceClient { get; set; }
        public ClientDictionariesServiceClientSettings ClientDictionariesServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { set; get; }
        public FeeCalculatorSettings FeeCalculatorServiceClient { set; get; }
        public FeeSettings FeeSettings { set; get; }
        public IcoSettings IcoSettings { get; set; }
        public GlobalSettings GlobalSettings { get; set; }
        public KycServiceClientSettings KycServiceClient { get; set; }
        public LimitationServiceSettings LimitationServiceClient { get; set; }
        public AssetDisclaimersServiceClientSettings AssetDisclaimersServiceClient { get; set; }
        public PaymentSystemServiceClientSettings PaymentSystemServiceClient { set; get; }
        public ClientDialogsServiceClientSettings ClientDialogsServiceClient { set; get; }
        public SwiftCredentialsServiceClientSettings SwiftCredentialsServiceClient { set; get; }
        public ConfirmationCodesServiceClientSettings ConfirmationCodesClient { set; get; }
        public SagasRabbitMq SagasRabbitMq { set; get; }
        public SwaggerSettings SwaggerSettings { get; set; }
        public Lykke.Service.BlockchainSettings.Client.BlockchainSettingsServiceClientSettings BlockchainSettingsServiceClient { get; set; }
    }

    public class SwaggerSettings
    {
        public SwaggerSecuritySettings Security { get; set; }
    }

    public class SwaggerSecuritySettings
    {
        public string OAuthClientId { get; set; }
        public string AuthorizeEndpoint { get; set; }
    }


    public class OAuthSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }
    }

    public class SagasRabbitMq
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
        public string RetryDelay { get; set; }
    }

    public class LimitationServiceSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class BlockchainWalletsServiceClientSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class GlobalSettings
    {
        public string[] BlockedAssetPairs { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool BtcOperationsDisabled { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }

    public class AzureQueueSettings
    {
        public string ConnectionString { get; set; }
        public string QueueName { get; set; }
    }

    public class BaseSettings
    {
        public DbSettings Db { get; set; }
        public ServiceSettings Services { get; set; }
        public DeploymentSettings DeploymentSettings { get; set; }
        public CacheSettings CacheSettings { get; set; }
        public CashoutSettings CashoutSettings { get; set; }
        public bool EnableFees { get; set; }
        public bool EnableSessionValidation { get; set; }
        public OAuthSettings OAuthSettings { get; set; }
        public bool EnableTwoFactor { get; set; }
        public int MaxTwoFactorConfirmationAttempts { get; set; }
        [Optional]
        public bool? IsMtDisabled { get; set; }
    }

    public class IcoSettings
    {
        public string LKK2YAssetId { get; set; }
        public string[] RestrictedCountriesIso3 { get; set; }
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string DataConnString { get; set; }
    }

    public class ServiceSettings
    {
        public string AssetsServiceUrl { get; set; }
        public string ClientAccountServiceUrl { get; set; }
        public string RegistrationUrl { get; set; }
        public string RateCalculatorServiceApiUrl { get; set; }
        public string BalancesServiceUrl { get; set; }
        public string MarketProfileUrl { get; set; }
        public string CandleHistorySpotUrl { get; set; }
        public string CandleHistoryMtUrl { get; set; }
        public string HftInternalServiceUrl { get; set; }
        public string SessionUrl { get; set; }
        public string OperationsUrl { get; set; }
        public string HistoryServiceUrl { get; set; }
        public AffiliateServiceClientSettings AffiliateServiceClient { get; set; }
        public string IndicesFacadeServiceUrl { get; set; }
    }

    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }

    public class FeeCalculatorSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class PaymentSystemSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class FeeSettings
    {
        public TargetClientIdFeeSettings TargetClientId { get; set; }
    }

    public class TargetClientIdFeeSettings
    {
        public string WalletApi { get; set; }
        public string Cashout { get; set; }
        public string Withdrawal { get; set; }
    }

    public class IpEndpointSettings
    {
        public string InternalHost { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint(bool useInternal = false)
        {
            string host = useInternal ? InternalHost : Host;

            if (IPAddress.TryParse(host, out var ipAddress))
                return new IPEndPoint(ipAddress, Port);

            var addresses = Dns.GetHostAddressesAsync(host).Result;
            return new IPEndPoint(addresses[0], Port);
        }
    }

    public class DeploymentSettings
    {
        public bool IsProduction { get; set; }
    }

    public class CacheSettings
    {
        public string FinanceDataCacheInstance { get; set; }
        public string RedisConfiguration { get; set; }
        public string OrderBooksCacheKeyPattern { get; set; }
    }

    public class CashoutSettings
    {
        public string SwiftHotwallet { get; set; }
    }

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }
}

