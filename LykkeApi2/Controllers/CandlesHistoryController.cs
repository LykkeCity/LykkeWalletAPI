using Common.Log;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Candles;
using Core.Enumerators;
using LkeServices.Candles;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;

namespace LykkeApi2.Controllers
{
    [Route("api/candlesHistory")]
    [ValidateModel]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesHistoryServiceProvider _candlesServiceProvider;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairs;
        private readonly IAssetsService _assetsService;
        private readonly ILog _log;

        public CandlesHistoryController(
            ICandlesHistoryServiceProvider candlesServiceProvider,
            IAssetsService assetsService,
            CachedDataDictionary<string, AssetPair> assetPairs,
            ILog log)
        {
            _candlesServiceProvider = candlesServiceProvider;
            _assetsService = assetsService;
            _assetPairs = assetPairs;
            _log = log;
        }

        /// <summary>
        /// AssetPairs candles history
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (exclusive)</param>
        [HttpGet("{type}/{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        [ProducesResponseType(typeof(CandleSticksResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get([FromRoute]CandleSticksRequestModel request)
        {
            try
            {
                var assetPair = (await _assetPairs.Values()).FirstOrDefault(x => x.Id == request.AssetPairId);

                if (assetPair == null)
                    return NotFound("Asset pair not found");
                
                var candleHistoryService = _candlesServiceProvider.Get(request.Type);
                
                var candles = await candleHistoryService.GetCandlesHistoryAsync(
                    request.AssetPairId,
                    (CandlePriceType) Enum.Parse(typeof(CandlePriceType), request.PriceType.ToString()),
                    (CandleTimeInterval) Enum.Parse(typeof(CandleTimeInterval), request.TimeInterval.ToString()),
                    request.FromMoment,
                    request.ToMoment);

                var baseAsset = await _assetsService.AssetGetAsync(assetPair.BaseAssetId);

                var quotingAsset = await _assetsService.AssetGetAsync(assetPair.QuotingAssetId);

                return Ok(
                    candles.ToResponseModel(
                        baseAsset.DisplayAccuracy ?? baseAsset.Accuracy,
                        quotingAsset.DisplayAccuracy ?? quotingAsset.Accuracy));
            }
            catch (ErrorResponseException ex)
            {
                var errors = ex.Error.ErrorMessages.Values.SelectMany(s => s.Select(ss => ss));
                return NotFound($"{string.Join(',', errors)}");
            }
        }
    }
}
