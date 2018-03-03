using System.Net;
using LkeServices.Settings;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.Session.Client;

namespace LykkeApi2.Settings
{
    public class APIv2Settings
    {
        public BaseSettings WalletApiv2 { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public PersonalDataServiceSettings PersonalDataServiceSettings { get; set; }
        public OperationsHistoryServiceClientSettings OperationsHistoryServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { set; get; }        
        public SessionsSettings SessionsSettings { get; set; }
        public FeeCalculatorSettings FeeCalculatorServiceClient { set; get; }
        public FeeSettings FeeSettings { set; get; }
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

        public RabbitMqSettings RabbitMq { get; set; }

        public bool EnableFees { get; set; }
    }

    public class RabbitMqSettings
    {
        public string ConnectionString { get; set; }
    }
    
    public class DbSettings
    {
        public string LogsConnString { get; set; }               
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
        public AffiliateServiceClientSettings AffiliateServiceClient { get; set; }
    }

    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }
    
    public class FeeCalculatorSettings
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
}
