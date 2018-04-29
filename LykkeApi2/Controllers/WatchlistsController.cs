using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using LkeServices;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Watchlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/watchlists")]
    public class WatchlistsController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IAssetsHelper _assetsHelper;

        public WatchlistsController(
            IRequestContext requestContext,
            IAssetsHelper assetsHelper
        )
        {
            _requestContext = requestContext;
            _assetsHelper = assetsHelper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WatchList>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetWatchlists()
        {
            var watchlists = await GetAllWatchlists();
            return Ok(watchlists);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WatchList), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var watchList = await GetWatchList(id);

            if (watchList == null)
                return NotFound();

            return Ok(watchList);
        }

        [HttpPost]
        [ProducesResponseType(typeof(WatchList), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] WatchListCreateModel model)
        {
            if (!await IsValidAsync(model.AssetIds) ||
                string.IsNullOrEmpty(model.Name))
                return BadRequest();

            var watchlists = await GetAllWatchlists();

            if (watchlists.Any(item => item.Name == model.Name))
                return BadRequest();

            var watchList = new WatchList
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                Order = model.Order,
                AssetIds = model.AssetIds.ToList()
            };

            var result = await _assetsHelper.AddCustomWatchListAsync(_requestContext.ClientId, watchList);

            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WatchList), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] WatchListUpdateModel model)
        {
            var watchList = await GetWatchList(id);

            if (watchList == null)
                return NotFound();

            if (watchList.ReadOnlyProperty ||
                !await IsValidAsync(model.AssetIds) ||
                string.IsNullOrEmpty(model.Name))
                return BadRequest();

            var watchlists = await GetAllWatchlists();

            if (watchlists.Any(item => item.Name == model.Name && item.Id != watchList.Id))
                return BadRequest();

            var newWatchList = new WatchList
            {
                Id = id,
                Name = model.Name,
                Order = model.Order,
                AssetIds = model.AssetIds.ToList()
            };

            await _assetsHelper.UpdateCustomWatchListAsync(_requestContext.ClientId, watchList);

            return Ok(newWatchList);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var watchList = await GetWatchList(id);

            if (watchList == null)
                return NotFound();

            if (watchList.ReadOnlyProperty)
                return BadRequest();

            await _assetsHelper.RemoveCustomWatchListAsync(_requestContext.ClientId, id);
            return Ok();
        }

        private async Task<WatchList> GetWatchList(string id)
        {
            var result = await _assetsHelper.GetCustomWatchListAsync(id, _requestContext.ClientId) ??
                         await _assetsHelper.GetPredefinedWatchListAsync(id);

            if (result == null)
            {
                return null;
            }

            var watchList = await FilterAssetsAsync(result);

            if (watchList == null)
            {
                throw new Exception("Assets in the watch-list are not accessable!");
            }

            return watchList;
        }

        private async Task<IEnumerable<WatchList>> GetAllWatchlists()
        {
            var availableAssetIds = await GetAvailableAssetIdsAsync();

            return (await _assetsHelper.GetAllCustomWatchListsForClient(_requestContext.ClientId))
                .Select(x => FilterAssets(x, availableAssetIds))
                .Where(x => x != null);
        }

        private async Task<bool> IsValidAsync(IEnumerable<string> assetIds, List<string> availableAssetIds = null)
        {
            var assets = assetIds.ToArray();

            if (!assets.Any() || assets.Any(string.IsNullOrEmpty))
                return false;

            availableAssetIds = availableAssetIds ?? await GetAvailableAssetIdsAsync();

            return assets.Where(x => !string.IsNullOrEmpty(x))
                .All(id => availableAssetIds.Contains(id));
        }

        private WatchList FilterAssets(WatchList watchList, List<string> availableAssetIds = null)
        {
            return FilterAssetsAsync(watchList, availableAssetIds).GetAwaiter().GetResult();
        }

        private async Task<WatchList> FilterAssetsAsync(WatchList watchList, List<string> availableAssetIds = null)
        {
            availableAssetIds = availableAssetIds ?? await GetAvailableAssetIdsAsync();

            var filteredAssetIds = watchList.AssetIds
                .Where(x => availableAssetIds.Contains(x))
                .ToList();

            if (!filteredAssetIds.Any())
            {
                return null;
            }

            var result = new WatchList
            {
                AssetIds = filteredAssetIds,
                Id = watchList.Id,
                Name = watchList.Name,
                Order = watchList.Order,
                ReadOnlyProperty = watchList.ReadOnlyProperty
            };

            return result;
        }

        private async Task<List<string>> GetAvailableAssetIdsAsync()
        {
            var assets =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId,
                    true);

            return assets.ToList();
        }
    }
}