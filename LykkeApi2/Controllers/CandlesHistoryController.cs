using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Custom;
using LykkeApi2.Models;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Candles;
using Core.Services;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models.CandleSticks;

namespace LykkeApi2.Controllers
{
    [Route("api/candlesHistory")]
    [ApiController]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesHistoryServiceProvider _candlesServiceProvider;
        private readonly IAssetsHelper _assetsHelper;

        public CandlesHistoryController(
            ICandlesHistoryServiceProvider candlesServiceProvider,
            IAssetsHelper assetsHelper)
        {
            _candlesServiceProvider = candlesServiceProvider;
            _assetsHelper = assetsHelper;
        }

        /// <summary>
        /// AssetPairs candles history (deprecated)
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (exclusive)</param>
        [Obsolete]
        [HttpGet("{type}/{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        [ProducesResponseType(typeof(CandleSticksResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.NotFound)]
        public Task<IActionResult> Get([FromRoute]CandleSticksRequestModel request)
        {
            return GetCandles(request);
        }

        /// <summary>
        /// AssetPairs candles history
        /// </summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(CandleSticksResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCandles([FromQuery]CandleSticksRequestModel request)
        {
            try
            {
                request.PriceType = CandlePriceType.Mid;
                var assetPair = await _assetsHelper.GetAssetPairAsync(request.AssetPairId);

                if (assetPair == null)
                    return NotFound("Asset pair not found");

                var candleHistoryService = _candlesServiceProvider.Get(request.Type);

                var candles = await candleHistoryService.GetCandlesHistoryAsync(
                    request.AssetPairId,
                    request.PriceType,
                    request.TimeInterval,
                    request.FromMoment,
                    request.ToMoment);

                var baseAsset = await _assetsHelper.GetAssetAsync(assetPair.BaseAssetId);

                var quotingAsset = await _assetsHelper.GetAssetAsync(assetPair.QuotingAssetId);

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
