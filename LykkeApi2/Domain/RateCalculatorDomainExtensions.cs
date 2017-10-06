using Lykke.Service.RateCalculator.Client.AutorestClient.Models;

namespace LykkeApi2.Domain
{
    public static class RateCalculatorDomainExtensions
    {
        public static OrderAction ToRateCalculatorDomain(this Core.Exchange.OrderAction action)
        {
            return action == Core.Exchange.OrderAction.Buy ? OrderAction.Buy : OrderAction.Sell;
        }
    }
}
