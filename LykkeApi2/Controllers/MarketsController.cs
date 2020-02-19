using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Lykke.Exchange.Api.MarketData;
using Microsoft.AspNetCore.Mvc;
using MarketSlice = LykkeApi2.Models.Markets.MarketSlice;

namespace LykkeApi2.Controllers
{
    [Route("api/markets")]
    [ApiController]
    public class MarketsController : Controller
    {
        private readonly MarketDataService.MarketDataServiceClient _marketDataServiceClient;

        public MarketsController(
            MarketDataService.MarketDataServiceClient marketDataServiceClient
            )
        {
            _marketDataServiceClient = marketDataServiceClient;
        }

        /// <summary>
        /// Get actual market state for all registered asset pairs.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(MarketSlice[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get()
        {
            MarketDataResponse response = await _marketDataServiceClient.GetMarketDataAsync(new Empty());
            var result = response.Items.Select(x => new MarketSlice
            {
                AssetPair = x.AssetPairId,
                PriceChange24H = GetValue(x.PriceChange),
                Volume24H = GetValue(x.VolumeBase),
                LastPrice = GetValue(x.LastPrice),
                Bid = GetValue(x.Bid),
                Ask = GetValue(x.Ask),
                High = GetValue(x.High),
                Low = GetValue(x.Low)
            });

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

            var result = await _marketDataServiceClient.GetAssetPairMarketDataAsync(new MarketDataRequest{AssetPairId = assetPairId});

            if (result == null)
                return BadRequest("Market state is missing for given asset pair.");

            return Ok(new MarketSlice
            {
                AssetPair = result.AssetPairId,
                PriceChange24H = GetValue(result.PriceChange),
                Volume24H = GetValue(result.VolumeBase),
                LastPrice = GetValue(result.LastPrice),
                Bid = GetValue(result.Bid),
                Ask = GetValue(result.Ask),
                High = GetValue(result.High),
                Low = GetValue(result.Low)
            });
        }

        private decimal GetValue(string value)
        {
            return decimal.TryParse(value, out var result) ? result : 0m;
        }
    }
}
