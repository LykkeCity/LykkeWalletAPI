using Lykke.Service.RateCalculator.Client.AutorestClient.Models;

namespace LykkeApi2.Domain
{
    public static class RateCalculatorDomainExtensions
    {
        public static OrderAction ToRateCalculatorDomain(this Core.Enumerators.OrderAction action)
        {
            return action == Core.Enumerators.OrderAction.Buy ? OrderAction.Buy : OrderAction.Sell;
        }
    }
}
