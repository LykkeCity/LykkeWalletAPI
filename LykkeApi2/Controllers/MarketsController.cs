using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LkeServices;
using LykkeApi2.Models.Markets;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/markets")]
    [ApiController]
    public class MarketsController : Controller
    {
        private readonly MarketsCache _marketsCache;

        #region Initialization

        public MarketsController(MarketsCache marketsCache)
        {
            _marketsCache = marketsCache;
        }

        #endregion

        #region PublicApi

        /// <summary>
        /// Get actual market state for all registered asset pairs.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(MarketSlice[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get()
        {
            var result = await _marketsCache.Get();
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

            var data = await _marketsCache.Get();
            var marketState = data.FirstOrDefault(e => e.AssetPair == assetPairId);

            if (marketState == null)
                return BadRequest("Market state is missing for given asset pair.");

            return Ok(marketState);
        }

        #endregion
    }
}
