using Common.Log;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/candlesHistory")]
    [ValidateModel]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandleshistoryservice _candleHistoryService;
        private readonly ILog _log;

        public CandlesHistoryController(ICandleshistoryservice candleHistoryService, ILog log)
        {
            _candleHistoryService = candleHistoryService;
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
        [HttpGet("{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        public async Task<IActionResult> Get([FromRoute]CandleSticksRequestModel request)
        {
            try
            {
                var candles = await _candleHistoryService.GetCandlesHistoryAsync(request.AssetPairId, (CandlePriceType)Enum.Parse(typeof(CandlePriceType), request.PriceType.ToString()), (CandleTimeInterval)Enum.Parse(typeof(CandleTimeInterval), request.TimeInterval.ToString()), request.FromMoment, request.ToMoment);
                return Ok(candles);
            }
            catch (ErrorResponseException ex)
            {
                var errors = ex.Error.ErrorMessages.Values.SelectMany(s => s.Select(ss => ss));
                return NotFound($"{string.Join(',', errors)}");
            }
        }
    }
}
