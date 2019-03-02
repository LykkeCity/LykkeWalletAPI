using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Candles;
using Core.Enumerators;
using Lykke.Common.Log;
using Lykke.MarketProfileService.Client;
using Lykke.MarketProfileService.Client.Models;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models.Markets;
using Microsoft.AspNetCore.Mvc;

namespace LkeServices
{
    public class MarketsCache
    {
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly ICandlesHistoryServiceProvider _candlesHistoryProvider;
        private TimerTrigger _timer;
        private List<MarketSlice> _cache = null;

        private static readonly HttpClient _client = new HttpClient();
        private static List<MarketSlice> _data = null;
        private static DateTime _dataTs = DateTime.UtcNow;


        public MarketsCache(ILykkeMarketProfileServiceAPI marketProfileService,
            ICandlesHistoryServiceProvider candlesHistoryProvider, ILog log)
        {
            _marketProfileService = marketProfileService;
            _candlesHistoryProvider = candlesHistoryProvider;
            _timer = new TimerTrigger(nameof(MarketsCache), TimeSpan.FromSeconds(5), log, DoTimer);
            _timer.Start();
        }

        private async Task DoTimer(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
        {
            await Update();
        }

        public async Task<List<MarketSlice>> Get()
        {
            if (_cache == null)
                await Update();

            return _cache;
        }

        public async Task Update()
        {
            var result = await GetSpotMarketSnapshotAsync();
            var lykke = await GetFromLykke();
            foreach (var slice in result)
            {
                slice.LastPrice = (slice.Ask + slice.Bid) / 2;

                if (slice.AssetPair == "BTCUSD" || slice.AssetPair == "BTCEMAAR")
                {
                    var p = lykke?.FirstOrDefault(e => e.AssetPair == "BTCUSD");
                    if (p != null)
                    {
                        slice.PriceChange24H = p.PriceChange24H;
                    }
                }

                if (slice.AssetPair == "ETHUSD" || slice.AssetPair == "ETHEMAAR")
                {
                    var p = lykke?.FirstOrDefault(e => e.AssetPair == "ETHUSD");
                    if (p != null)
                    {
                        slice.PriceChange24H = p.PriceChange24H;
                    }
                }
            }

            _cache = result;
        }

        private async Task<List<MarketSlice>> GetFromLykke()
        {
            if ((DateTime.UtcNow - _dataTs).TotalMinutes > 2 || _data == null)
            {
                var resp = await _client.GetAsync("https://apiv2.lykke.com/api/markets");
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    _data = json.DeserializeJson<List<MarketSlice>>();
                    _dataTs = DateTime.UtcNow;
                }
            }

            return _data;
        }

        /// <summary>
        /// Gathers generalized information about the current state of the Spot market.
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID. If not specified (is null or empty string), there will be gathered the info about all the registered asset pairs.</param>
        private async Task<List<MarketSlice>> GetSpotMarketSnapshotAsync(string assetPairId = null)
        {
            var marketProfiles = await GetMarketProfilesAsync(assetPairId);
            var todayCandles = await GetTodaySpotCandlesAsync(assetPairId); // May have (and usually does) a different count of returned records than from market profile query.
            var lastMonthCandles = await GetLastSpotCandlesAsync(assetPairId); // The last of existing month candle(s) is(are) taken, if any.

            var result = new Dictionary<string, MarketSlice>();

            // AssetPair & Bid & Ask
            foreach (var marketProfile in marketProfiles)
            {
                result[marketProfile.AssetPair] = new MarketSlice
                {
                    AssetPair = marketProfile.AssetPair,
                    Bid = (decimal)marketProfile.BidPrice,
                    Ask = (decimal)marketProfile.AskPrice
                };
            }

            // Volume24 & PriceChange24
            foreach (var todayCandle in todayCandles)
            {
                var candleValue = todayCandle.Value;
                var priceChange24 =
                    candleValue.Open > 0
                    ? (decimal)((candleValue.Close - candleValue.Open) / candleValue.Open)
                    : 0;

                if (result.TryGetValue(todayCandle.Key, out var existingAssetRecord))
                {
                    existingAssetRecord.Volume24H = (decimal)candleValue.TradingVolume;
                    existingAssetRecord.PriceChange24H = priceChange24;
                }
                else
                {
                    result[todayCandle.Key] = new MarketSlice
                    {
                        AssetPair = todayCandle.Key,
                        Volume24H = (decimal)candleValue.TradingVolume,
                        PriceChange24H = priceChange24,
                    };
                }
            }

            // LastPrice
            foreach (var monthCandle in lastMonthCandles)
            {
                var candleValue = monthCandle.Value;

                if (result.TryGetValue(monthCandle.Key, out var existingAssetRecord))
                    existingAssetRecord.LastPrice = (decimal)candleValue.Close;
                else
                    result[monthCandle.Key] = new MarketSlice
                    {
                        AssetPair = monthCandle.Key,
                        LastPrice = (decimal)candleValue.Close
                    };
            }

            return result.Values.ToList();
        }

        /// <summary>
        /// Gets (a set of) market profile(s).
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID. If not specified (is null or empty string), there will be gathered the info about all the registered asset pairs.</param>
        private async Task<List<AssetPairModel>> GetMarketProfilesAsync(string assetPairId = null)
        {
            var marketProfiles = new List<AssetPairModel>();

            if (!string.IsNullOrWhiteSpace(assetPairId))
            {
                var marketProfile = await _marketProfileService.TryGetAssetPairAsync(assetPairId);
                if (marketProfile == null)
                    throw new InvalidOperationException($"Asset pair {assetPairId} is is not registered.");

                marketProfiles.Add(await _marketProfileService.GetAssetPairAsync(assetPairId));
            }
            else
                marketProfiles.AddRange(await _marketProfileService.ApiMarketProfileGetAsync());

            return marketProfiles;
        }

        /// <summary>
        /// Gets (a set of) today's Day candle(s) of type Trades and Spot market.
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID. If not specified (is null or empty string), there will be gathered the info about all the registered asset pairs.</param>
        /// <returns>A dictionary where the Key is the asset pair ID and the Value contains the today's Day Spot candle for the asset pair.</returns>
        /// <remarks>When there is no Day Spot Trade candle for some asset pair, it will not be presented in the resulting dictionary. Thus, if assetPairId parameter is specified
        /// but there is no a suitable candle for it, the method will return an empty dictionary.</remarks>
        private async Task<Dictionary<string, Candle>> GetTodaySpotCandlesAsync(string assetPairId = null)
        {
            var historyService = _candlesHistoryProvider.Get(MarketType.Spot);

            var todayCandles = new Dictionary<string, Candle>();

            var dateFromInclusive = DateTime.UtcNow.Date;
            var dateToExclusive = dateFromInclusive.AddDays(1);

            if (!string.IsNullOrWhiteSpace(assetPairId))
            {
                var todayCandleHistory = await historyService.TryGetCandlesHistoryAsync(assetPairId,
                    CandlePriceType.Trades, CandleTimeInterval.Day, dateFromInclusive, dateToExclusive);

                if (todayCandleHistory?.History == null ||
                    !todayCandleHistory.History.Any())
                    return todayCandles;

                if (todayCandleHistory.History.Count > 1) // The unbelievable case.
                    throw new AmbiguousMatchException($"It seems like we have more than one today's Day Spot trade candle for asset pair {assetPairId}.");

                todayCandles.Add(
                    assetPairId,
                    todayCandleHistory
                        .History
                        .Single()
                    );
            }
            else
            {
                var assetPairs = await historyService.GetAvailableAssetPairsAsync();
                var todayCandleHistoryForPairs = await historyService.GetCandlesHistoryBatchAsync(assetPairs,
                    CandlePriceType.Trades, CandleTimeInterval.Day, dateFromInclusive, dateToExclusive);

                if (todayCandleHistoryForPairs == null) // Some technical issue has happened without an exception.
                    throw new InvalidOperationException("Could not obtain today's Day Spot trade candles at all.");

                if (!todayCandleHistoryForPairs.Any())
                    return todayCandles;

                foreach (var historyForPair in todayCandleHistoryForPairs)
                {
                    if (historyForPair.Value?.History == null ||
                        !historyForPair.Value.History.Any())
                        continue;

                    if (historyForPair.Value.History.Count > 1) // The unbelievable case.
                        throw new AmbiguousMatchException($"It seems like we have more than one today's Day Spot trade candle for asset pair {assetPairId}.");

                    todayCandles.Add(
                        historyForPair.Key,
                        historyForPair.Value
                            .History
                            .Single()
                        );
                }
            }

            return todayCandles;
        }

        /// <summary>
        /// Gets (a set of) month candle(s) of type Trades and Spot market. The search depth is 12 months.
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID. If not specified (is null or empty string), there will be gathered the info about all the registered asset pairs.</param>
        /// <returns>A dictionary where the Key is the asset pair ID and the Value contains the last of existing month Spot candle for the asset pair for the last year.</returns>
        /// <remarks>When there is no a month Spot Trade candle for some asset pair, it will not be presented in the resulting dictionary. Thus, if assetPairId parameter is specified
        /// but there is no a suitable candle for it, the method will return an empty dictionary.</remarks>
        private async Task<Dictionary<string, Candle>> GetLastSpotCandlesAsync(string assetPairId = null)
        {
            var historyService = _candlesHistoryProvider.Get(MarketType.Spot);

            var monthCandles = new Dictionary<string, Candle>();

            var today = DateTime.UtcNow.Date;
            var dateFromInclusive = today.AddYears(-1);
            var dateToExclusive = today.AddMonths(1);

            if (!string.IsNullOrWhiteSpace(assetPairId))
            {
                var monthCandleHistory = await historyService.TryGetCandlesHistoryAsync(assetPairId,
                    CandlePriceType.Trades, CandleTimeInterval.Month, dateFromInclusive, dateToExclusive);

                if (monthCandleHistory?.History == null ||
                    !monthCandleHistory.History.Any())
                    return monthCandles;

                monthCandles.Add(
                    assetPairId,
                    monthCandleHistory
                        .History
                        .Last()
                    );
            }
            else
            {
                var assetPairs = await historyService.GetAvailableAssetPairsAsync();
                var monthCandleHistoryForPairs = await historyService.GetCandlesHistoryBatchAsync(assetPairs,
                    CandlePriceType.Trades, CandleTimeInterval.Month, dateFromInclusive, dateToExclusive);

                if (monthCandleHistoryForPairs == null) // Some technical issue has happened without an exception.
                    throw new InvalidOperationException("Could not obtain month Spot trade candles at all.");

                if (!monthCandleHistoryForPairs.Any())
                    return monthCandles;

                foreach (var historyForPair in monthCandleHistoryForPairs)
                {
                    if (historyForPair.Value?.History == null ||
                        !historyForPair.Value.History.Any())
                        continue;

                    monthCandles.Add(
                        historyForPair.Key,
                        historyForPair.Value
                            .History
                            .Last()
                        );
                }
            }

            return monthCandles;
        }


    }
}