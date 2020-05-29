using System.Net;
using System.Threading.Tasks;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Mvc;
using MarketSlice = LykkeApi2.Models.Markets.MarketSlice;

namespace LykkeApi2.Controllers
{
    [Route("api/markets")]
    [ApiController]
    public class MarketsController : Controller
    {
        private readonly MarketDataCacheService _marketDataService;

        public MarketsController(
            MarketDataCacheService marketDataService
            )
        {
            _marketDataService = marketDataService;
        }

        /// <summary>
        /// Get actual market state for all registered asset pairs.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(MarketSlice[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public IActionResult Get()
        {
            var marketData = _marketDataService.GetAll();
            return Ok(marketData);
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

            var result = _marketDataService.Get(assetPairId);

            if (result == null)
                return BadRequest("Market state is missing for given asset pair.");

            return Ok(result);
        }
    }
}
