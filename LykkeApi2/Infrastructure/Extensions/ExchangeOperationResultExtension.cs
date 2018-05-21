using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.ExchangeOperations.Client.AutorestClient.Models;

namespace LykkeApi2.Infrastructure.Extensions
{
    public static class ExchangeOperationResultExtension
    {
        public static bool IsDuplicate(this ExchangeOperationResult result)
        {
            return result.Code == (int) MeStatusCodes.Duplicate;
        }
    }
}