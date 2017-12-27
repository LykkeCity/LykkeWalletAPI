using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Core.Domain.Orderbook;
using Core.Services;
using Lykke.Service.Assets.Client.Models;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class OrderbookController : Controller
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetPairs;
        private readonly IOrderBooksService _orderBooksService;

        public OrderbookController(
            CachedDataDictionary<string, AssetPair> assetPairs,
            IOrderBooksService orderBooksService
            )
        {
            _assetPairs = assetPairs;
            _orderBooksService = orderBooksService;
        }
        [HttpGet]
        public async Task<IActionResult> Get(string assetPairId = null)
        {
            assetPairId = string.IsNullOrEmpty(assetPairId)
                ? null
                : assetPairId.ToUpper();

            AssetPair pair = null;

            if (!string.IsNullOrEmpty(assetPairId))
                pair = await _assetPairs.GetItemAsync(assetPairId);

            if (!string.IsNullOrEmpty(assetPairId) && pair == null)
                return NotFound($"Asset pair {assetPairId} not found");

            IEnumerable<IOrderBook> result = string.IsNullOrEmpty(assetPairId) 
                ? await _orderBooksService.GetAllAsync()
                : await _orderBooksService.GetAsync(assetPairId);

            return Ok(result);
        }
    }
}
