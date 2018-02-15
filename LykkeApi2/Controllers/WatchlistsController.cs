using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        private readonly IAssetsService _assetsService;
        private readonly SrvAssetsHelper _srvAssetsHelper;

        public WatchlistsController(
            IRequestContext requestContext,
            IAssetsService assetsService,
            SrvAssetsHelper srvAssetsHelper
        )
        {
            _requestContext = requestContext;
            _assetsService = assetsService;
            _srvAssetsHelper = srvAssetsHelper;
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
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                WatchList watchList = await GetWatchList(id);
                
                if (watchList == null)
                    return NotFound("Watch-list not found!");

                return Ok(watchList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(WatchList), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] WatchListCreateModel model)
        {
            if (!await IsValidAsync(model.AssetIds))
                return BadRequest("Wrong assets in 'AssetIds' list");
            
            if (string.IsNullOrEmpty(model.Name))
                return BadRequest("Name can't be empty");

            var watchlists = await GetAllWatchlists();

            if (watchlists.Any(item => item.Name == model.Name))
                return BadRequest($"Watch-list with name '{model.Name}' already exists");

            var watchList = new WatchList
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                Order = model.Order,
                AssetIds = model.AssetIds.ToList()
            };

            var result = await _assetsService.WatchListAddCustomAsync(watchList, _requestContext.ClientId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WatchList), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] WatchListUpdateModel model)
        {
            try
            {
                WatchList watchList = await GetWatchList(id);
                
                if (watchList == null)
                    return NotFound("Watch-list not found!");
                
                if (watchList.ReadOnlyProperty)
                    return BadRequest("This watch-list is read only!");

                if (!await IsValidAsync(model.AssetIds))
                    return BadRequest("Wrong assets in 'AssetIds' list");

                if (string.IsNullOrEmpty(model.Name))
                    return BadRequest("Name can't be empty");

                var watchlists = await GetAllWatchlists();

                if (watchlists.Any(item => item.Name == model.Name && item.Id != watchList.Id))
                    return BadRequest($"Watch-list with name '{model.Name}' already exists");

                var newWatchList = new WatchList
                {
                    Id = id,
                    Name = model.Name,
                    Order = model.Order,
                    AssetIds = model.AssetIds.ToList()
                };

                await _assetsService.WatchListUpdateCustomAsync(newWatchList, _requestContext.ClientId);

                return Ok(newWatchList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                WatchList watchList = await GetWatchList(id);
                
                if (watchList == null)
                    return NotFound("Watch-list not found!");
                
                if (watchList.ReadOnlyProperty)
                    return BadRequest("This watch-list is read only!");

                await _assetsService.WatchListCustomRemoveAsync(id, _requestContext.ClientId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<WatchList> GetWatchList(string id)
        {
            var result = await _assetsService.WatchListGetCustomAsync(id, _requestContext.ClientId) ?? 
                         await _assetsService.WatchListGetPredefinedAsync(id);

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

            return (await _assetsService.WatchListGetAllAsync(_requestContext.ClientId))
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
            return (await _srvAssetsHelper
                    .GetAssetsPairsForClient(_requestContext.ClientId, _requestContext.IsIosDevice,
                        _requestContext.PartnerId, true))
                .Select(x => x.Id)
                .ToList();
        }
    }
}
