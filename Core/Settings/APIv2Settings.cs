using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.PersonalData.Settings;

namespace Core.Settings
{
    public class APIv2Settings
    {
        public BaseSettings WalletApiv2 { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public PersonalDataServiceSettings PersonalDataServiceSettings { get; set; }
        public OperationsHistoryServiceClientSettings OperationsHistoryServiceClient { get; set; }
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
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }        
        public string ClientPersonalInfoConnString { get; set; }        
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

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }
}
