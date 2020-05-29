using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Google.Protobuf.WellKnownTypes;
using Lykke.Exchange.Api.MarketData;
using MarketSlice = LykkeApi2.Models.Markets.MarketSlice;

namespace LykkeApi2.Services
{
    public class MarketDataCacheService : IStartable, IStopable
    {
        private readonly MarketDataService.MarketDataServiceClient _marketDataServiceClient;
        private readonly TimerTrigger _timerTrigger;
        private readonly ConcurrentDictionary<string, MarketSlice> _cache = new ConcurrentDictionary<string, MarketSlice>();

        public MarketDataCacheService(
            MarketDataService.MarketDataServiceClient marketDataServiceClient,
            ILog log
            )
        {
            _marketDataServiceClient = marketDataServiceClient;
            _timerTrigger = new TimerTrigger(nameof(MarketDataCacheService), TimeSpan.FromSeconds(10), log);
            _timerTrigger.Triggered += Execute;

        }

        public void Start()
        {
            _timerTrigger.Start();
        }

        public void Stop()
        {
            Console.WriteLine("Stop timer...");
            _timerTrigger.Stop();
        }

        public void Dispose()
        {
            _timerTrigger.Stop();
            _timerTrigger.Dispose();
        }

        public IReadOnlyList<MarketSlice> GetAll()
        {
            return _cache.Values.ToList();
        }

        public MarketSlice Get(string assetPairId)
        {
            _cache.TryGetValue(assetPairId, out var result);
            return result;
        }

        private Task Execute(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationToken)
        {
            return UpdateCacheAsync();
        }

        private decimal GetValue(string value)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0m;
        }

        private async Task UpdateCacheAsync()
        {
            var marketData = await _marketDataServiceClient.GetMarketDataAsync(new Empty());

            var result = marketData.Items.Select(x => new MarketSlice
            {
                AssetPair = x.AssetPairId,
                PriceChange24H = GetValue(x.PriceChange),
                Volume24H = GetValue(x.VolumeBase),
                LastPrice = GetValue(x.LastPrice),
                Bid = GetValue(x.Bid),
                Ask = GetValue(x.Ask),
                High = GetValue(x.High),
                Low = GetValue(x.Low)
            });

            foreach (var item in result)
                _cache[item.AssetPair] = item;
        }
    }
}
