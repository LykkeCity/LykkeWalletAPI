using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Candles;
using Core.Enumerators;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
using Lykke.Service.MarketProfile.Client;
using Lykke.Service.MarketProfile.Client.Models;
using LykkeApi2.Models.Markets;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/markets")]
    [ApiController]
    public class MarketsController : Controller
    {
        private readonly ILykkeMarketProfile _marketProfileService;
        private readonly ICandlesHistoryServiceProvider _candlesHistoryProvider;

        #region Initialization

        public MarketsController(ILykkeMarketProfile marketProfileService,
            ICandlesHistoryServiceProvider candlesHistoryProvider)
        {
            _marketProfileService = marketProfileService ?? throw new ArgumentNullException(nameof(marketProfileService));
            _candlesHistoryProvider = candlesHistoryProvider ?? throw new ArgumentNullException(nameof(candlesHistoryProvider));
        }

        #endregion

        #region PublicApi

        /// <summary>
        /// Get actual market state for all registered asset pairs.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(MarketSlice[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get()
        {
            var result = await GetSpotMarketSnapshotAsync();

            return Ok(result);
        }

        /// <summary>
        /// Get actual market state for the given asset pair.
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID.</param>
        [HttpGet("{assetPairId}")]
        [ProducesResponseType(typeof(MarketSlice), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(string assetPairId)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
                return BadRequest("Please, specify the target asset pair id.");

            var result = await GetSpotMarketSnapshotAsync(assetPairId);

            var marketState = result.FirstOrDefault();

            if (marketState == null)
                return BadRequest("Market state is missing for given asset pair.");

            return Ok(marketState);
        }

        #endregion

        #region Private

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
                if (result.TryGetValue(todayCandle.AssetPair, out var existingAssetRecord))
                {
                    existingAssetRecord.Volume24H = todayCandle.Volume24H;
                    existingAssetRecord.PriceChange24H = todayCandle.PriceChange24H;
                }
                else
                {
                    result[todayCandle.AssetPair] = todayCandle;
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
                var result = await _marketProfileService.ApiMarketProfileByPairCodeGetAsync(assetPairId);
                var marketProfile = result is AssetPairModel m ? m : null;
                marketProfiles.Add(
                    marketProfile ?? new AssetPairModel { AssetPair = assetPairId });
            }
            else
            {
                marketProfiles.AddRange(await _marketProfileService.ApiMarketProfileGetAsync());
            }

            return marketProfiles;
        }

        /// <summary>
        /// Gets (a set of) today's hour candles of type Trades and Spot market.
        /// </summary>
        /// <param name="assetPairId">The target asset pair ID. If not specified (is null or empty string), there will be gathered the info about all the registered asset pairs.</param>
        /// <returns>A list or MarketSlice with summary today's hour candles volume and price change.</returns>
        /// <remarks>When there are no Hour Spot Trade candles for some asset pair, it will not be presented in the resulting list. Thus, if assetPairId parameter is specified
        /// but there is no a suitable candles for it, the method will return an empty list.</remarks>
        private async Task<List<MarketSlice>> GetTodaySpotCandlesAsync(string assetPairId = null)
        {
            var historyService = _candlesHistoryProvider.Get(MarketType.Spot);

            var todayCandles = new List<MarketSlice>();

            var now = DateTime.UtcNow;
            // inclusive
            var from = now - TimeSpan.FromHours(24);
            // exclusive
            var to = now; 

            if (!string.IsNullOrWhiteSpace(assetPairId))
            {
                var todayCandleHistory = await historyService.TryGetCandlesHistoryAsync(assetPairId,
                    CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

                if (todayCandleHistory?.History == null ||
                    !todayCandleHistory.History.Any())
                    return todayCandles;

                var firstCandle = todayCandleHistory.History.First();
                var lastCandle = todayCandleHistory.History.Last();

                var marketSlice = new MarketSlice
                {
                    AssetPair = assetPairId,
                    Volume24H = (decimal) todayCandleHistory.History.Sum(c => c.TradingOppositeVolume),
                    PriceChange24H = firstCandle.Open > 0
                        ? (decimal) ((lastCandle.Close - firstCandle.Open) / firstCandle.Open)
                        : 0
                };

                todayCandles.Add(marketSlice);
            }
            else
            {
                var assetPairs = await historyService.GetAvailableAssetPairsAsync();
                var todayCandleHistoryForPairs = await historyService.GetCandlesHistoryBatchAsync(assetPairs,
                    CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

                if (todayCandleHistoryForPairs == null) // Some technical issue has happened without an exception.
                    throw new InvalidOperationException("Could not obtain today's Hour Spot trade candles at all.");

                if (!todayCandleHistoryForPairs.Any())
                    return todayCandles;

                foreach (var historyForPair in todayCandleHistoryForPairs)
                {
                    if (historyForPair.Value?.History == null ||
                        !historyForPair.Value.History.Any())
                        continue;
                    
                    var firstCandle = historyForPair.Value.History.First();
                    var lastCandle = historyForPair.Value.History.Last();

                    var marketSlice = new MarketSlice
                    {
                        AssetPair = assetPairId,
                        Volume24H = (decimal) historyForPair.Value.History.Sum(c => c.TradingOppositeVolume),
                        PriceChange24H = firstCandle.Open > 0
                            ? (decimal) ((lastCandle.Close - firstCandle.Open) / firstCandle.Open)
                            : 0
                    };

                    todayCandles.Add(marketSlice);
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

        #endregion
    }
}
