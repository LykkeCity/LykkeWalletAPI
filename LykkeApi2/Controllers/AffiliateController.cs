using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.Affiliate.Contracts;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Affiliate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AffiliateController : Controller
    {
        private readonly IAffiliateClient _affiliateClient;
        private readonly IRequestContext _requestContext;
        private readonly IRateCalculatorClient _rateCalculatorClient;

        public AffiliateController(IAffiliateClient affiliateClient, IRequestContext requestContext, IRateCalculatorClient rateCalculatorClient)
        {
            _affiliateClient = affiliateClient;
            _requestContext = requestContext;
            _rateCalculatorClient = rateCalculatorClient;
        }

        [HttpGet]
        [Route("getlinks")]
        public async Task<IActionResult> Get()
        {
            var val = await _affiliateClient.GetLinks(_requestContext.ClientId);
            return Ok();
        }

        [HttpGet]
        [Route("stats")]
        [ProducesResponseType(typeof(AffiliateStatisticsResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Stats()
        {
            var referrals = await _affiliateClient.GetReferrals(_requestContext.ClientId);

            var stats = (await _affiliateClient.GetStats(_requestContext.ClientId)).ToList();

            var bonuses = await GetAmountInBtc(stats.ToDictionary(x => x.AssetId, x => x.BonusVolume));

            var tradeVolumes = await GetAmountInBtc(stats.ToDictionary(x => x.AssetId, x => x.TradeVolume));

            return Ok(new AffiliateStatisticsResponse
            {
                ReferralsCount = referrals.Count(),
                TotalBonus = bonuses,
                TotalTradeVolume = tradeVolumes
            });
        }


        private async Task<double> GetAmountInBtc(IReadOnlyDictionary<string, decimal> values)
        {
            var data = await _rateCalculatorClient.GetMarketAmountInBaseAsync(
                values.Where(x => x.Value > 0 && x.Key != LykkeConstants.BitcoinAssetId).Select(x => new AssetWithAmount((double)x.Value, x.Key)),
                LykkeConstants.BitcoinAssetId, OrderAction.Sell);

            var result = data.Sum(x => x.To?.Amount ?? 0);

            if (values.ContainsKey(LykkeConstants.BitcoinAssetId))
                result += (double)values[LykkeConstants.BitcoinAssetId];

            return result;
        }

    }
}