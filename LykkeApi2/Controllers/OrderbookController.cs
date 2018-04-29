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
        private readonly IAssetsHelper _assetsHelper;
        private readonly IOrderBooksService _orderBooksService;

        public OrderbookController(
            IAssetsHelper assetsHelper,
            IOrderBooksService orderBooksService)
        {
            _assetsHelper = assetsHelper;
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

            var assetPair = await _assetsHelper.GetAssetPairAsync(assetPairId);
                    
            if (assetPair == null || assetPair.IsDisabled)
                return NotFound();

            var baseAsset = await _assetsHelper.GetAssetAsync(assetPair.BaseAssetId);
            var quotingAsset = await _assetsHelper.GetAssetAsync(assetPair.QuotingAssetId);

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
