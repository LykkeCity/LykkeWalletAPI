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
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IRateCalculatorClient _rateCalculator;

        public MarketController(IRateCalculatorClient rateCalculator)
        {
            _rateCalculator = rateCalculator;
        }

        [HttpPost("converter")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(ConvertionResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Convert([FromBody] ConvertionRequest request)
        {
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