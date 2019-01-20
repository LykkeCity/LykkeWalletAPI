using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Exchange;
using Lykke.Service.RateCalculator.Client;
using LykkeApi2.Domain;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/market")]
    [ApiController]
    public class MarketController : Controller
    {
        private readonly IRateCalculatorClient _rateCalculator;

        public MarketController(IRateCalculatorClient rateCalculator)
        {
            _rateCalculator = rateCalculator;
        }

        /// <summary>
        /// Convert one asset to another.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("converter")]
        [ProducesResponseType(typeof(ConvertionResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Convert([FromBody] ConvertionRequest request)
        {
            if (request == null)
                return BadRequest("Request can't be empty");

            var orderAction = BaseOrderExt.GetOrderAction(request.OrderAction);
            if (orderAction == null)
            {
                ModelState.AddModelError("OrderAction", request.OrderAction);
                return BadRequest(ModelState);
            }

            var result = await _rateCalculator.GetMarketAmountInBaseAsync(request.AssetsFrom, request.BaseAssetId,
                orderAction.Value.ToRateCalculatorDomain());

            return Ok(new ConvertionResponse {Converted = result.ToArray()});
        }
    }
}
