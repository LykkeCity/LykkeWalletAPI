using Common.Log;
using Core.Enums;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Custom;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkeApi2.Models;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class CandleSticksController : Controller
    {
        private readonly ICandleshistoryservice _candleHistoryService;
        private readonly ILog _log;

        public CandleSticksController(ICandleshistoryservice candleHistoryService, ILog log)
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
        [HttpGet("{AssetPairId}/{PriceType}/{TimeInterval}/{FromMoment:datetime}/{ToMoment:datetime}")]
        public async Task<IActionResult> Get([FromRoute]CandleSticksRequestModel request) 
        {
            try
            {
                var candles = await _candleHistoryService.GetCandlesHistoryAsync(request.AssetPairId, (PriceType)Enum.Parse(typeof(PriceType), request.PriceType.ToString()), (TimeInterval)Enum.Parse(typeof(TimeInterval), request.TimeInterval.ToString()), request.FromMoment, request.ToMoment);
                return Ok(candles);
            }
            catch (ErrorResponseException ex)
            {
                var errors = ex.Error.ErrorMessages.Values.SelectMany(s => s.Select(ss => ss));
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, $"{String.Join(',', errors)}"));
            }
        }
    }
}
