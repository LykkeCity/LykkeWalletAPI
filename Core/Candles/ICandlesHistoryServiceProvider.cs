using Core.Enumerators;
using Lykke.Service.CandlesHistory.Client;

namespace Core.Candles
{
    public interface ICandlesHistoryServiceProvider
    {
        ICandleshistoryservice TryGet(MarketType market);
        ICandleshistoryservice Get(MarketType market);
    }
}