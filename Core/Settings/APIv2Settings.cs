using System.Net;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.PersonalData.Settings;

namespace Core.Settings
{
    public class APIv2Settings
    {
        public BaseSettings WalletApiv2 { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public PersonalDataServiceSettings PersonalDataServiceSettings { get; set; }
        public OperationsHistoryServiceClientSettings OperationsHistoryServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { set; get; }
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
        
        public OrdersSettings OrdersSettings { set; get; }
    }

    public class OrdersSettings
    {
        public double MaxLimitOrderDeviationPercent { set; get; }
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
        public OperationsRepositoryServiceClientSettings OperationsRepositoryClient { set; get; }
    }
    
    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
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

    public static class CacheSettingsExt
    {
        public static string GetOrderBookKey(this CacheSettings settings, string assetPairId, bool isBuy)
        {
            return string.Format(settings.OrderBooksCacheKeyPattern, assetPairId, isBuy);
        }
    }
}
