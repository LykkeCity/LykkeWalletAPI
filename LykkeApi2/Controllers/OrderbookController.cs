using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Domain.Orderbook;
using Core.Services;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class OrderbookController : Controller
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsCache;
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly IOrderBooksService _orderBooksService;

        public OrderbookController(
            CachedDataDictionary<string, AssetPair> assetPairsCache,
            CachedDataDictionary<string, Asset> assetsCache,
            IOrderBooksService orderBooksService
            )
        {
            _assetPairsCache = assetPairsCache;
            _assetsCache = assetsCache;
            _orderBooksService = orderBooksService;
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderBookModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string assetPairId = null)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
                return BadRequest();

            var assetPair = await _assetPairsCache.GetItemAsync(assetPairId);
                    
            if (assetPair == null || assetPair.IsDisabled)
                return NotFound();

            var baseAsset = await _assetsCache.GetItemAsync(assetPair.BaseAssetId);
            var quotingAsset = await _assetsCache.GetItemAsync(assetPair.QuotingAssetId);

            if (baseAsset == null || baseAsset.IsDisabled ||
                quotingAsset == null || quotingAsset.IsDisabled)
                return NotFound();

            if (!baseAsset.IsTradable || !quotingAsset.IsTradable)
                return BadRequest();

            var result = await _orderBooksService.GetAsync(assetPairId);

            return Ok(result.ToApiModel());
        }
    }
}
