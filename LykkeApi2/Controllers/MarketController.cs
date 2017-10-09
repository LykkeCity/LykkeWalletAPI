using System.Linq;
using System.Threading.Tasks;
using Core.Exchange;
using Lykke.Service.RateCalculator.Client;
using LykkeApi2.Domain;
using LykkeApi2.Models;
using LykkeApi2.Strings;
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
        public async Task<ResponseModel<ConvertionResponse>> Convert([FromBody] ConvertionRequest request)
        {
            var orderAction = BaseOrderExt.GetOrderAction(request.OrderAction);
            if (orderAction == null)
                return ResponseModel<ConvertionResponse>.CreateInvalidFieldError(request.OrderAction,
                    Phrases.InvalidValue);

            var result = await _rateCalculator.GetMarketAmountInBaseAsync(request.AssetsFrom, request.BaseAssetId,
                orderAction.Value.ToRateCalculatorDomain());

            return ResponseModel<ConvertionResponse>.CreateOk(new ConvertionResponse {Converted = result.ToArray()});
        }
    }
}