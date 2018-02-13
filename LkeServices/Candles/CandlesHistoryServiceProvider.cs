using System;
using System.Collections.Generic;
using Core.Candles;
using Core.Enumerators;
using Lykke.Service.CandlesHistory.Client;

namespace LkeServices.Candles
{
    public class CandlesHistoryServiceProvider : ICandlesHistoryServiceProvider
    {
        private readonly Dictionary<MarketType, ICandleshistoryservice> _services;

        public CandlesHistoryServiceProvider()
        {
            _services = new Dictionary<MarketType, ICandleshistoryservice>();
        }

        public void RegisterMarket(MarketType marketType, string connectionString)
        {
            _services.Add(marketType, new Candleshistoryservice(new Uri(connectionString)));
        }

        public ICandleshistoryservice TryGet(MarketType market)
        {
            _services.TryGetValue(market, out var service);

            return service;
        }

        public ICandleshistoryservice Get(MarketType market)
        {
            var service = TryGet(market);

            if (service == null)
            {
                throw new ArgumentOutOfRangeException(nameof(market), market, "Market not found");
            }

            return service;
        }
    }
}