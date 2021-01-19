using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antares.Service.Assets.Client;
using Antares.Service.Assets.Client.Models;
using Common.Cache;
using Core.Services;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Core.Domain;

namespace LkeServices
{
    [UsedImplicitly]
    public class AssetsHelper : IAssetsHelper
    {
        private readonly IAssetsServiceClient _assetsService;
        private readonly IAssetsServiceUserDataClient _assetsServiceUserDataClient;
        private readonly ICacheManager _memoryCache;
        private const string AssetsExtendedInfoKey = "AssetsExtendedInfo";
        private const int AssetsExtendedInfoCacheMins = 10;

        public AssetsHelper(
            IAssetsServiceClient assetsService,
            IAssetsServiceUserDataClient assetsServiceUserDataClient,
            ICacheManager memoryCache
            )
        {
            _assetsService = assetsService;
            _assetsServiceUserDataClient = assetsServiceUserDataClient;
            _memoryCache = memoryCache;
        }

        public Task<IAsset> GetAssetAsync(string assetId)
        {
            return Task.FromResult(_assetsService.Assets.Get(assetId));
        }

        public Task<IReadOnlyCollection<IAsset>> GetAllAssetsAsync()
        {
            var data = _assetsService.Assets.GetAll(true);
            return Task.FromResult((IReadOnlyCollection<IAsset>) data);
        }

        public Task<IAssetPair> GetAssetPairAsync(string assetPairId)
        {
            return Task.FromResult(_assetsService.AssetPairs.Get(assetPairId));
        }

        public Task<IReadOnlyCollection<IAssetPair>> GetAllAssetPairsAsync()
        {
            var data = _assetsService.AssetPairs.GetAll();
            return Task.FromResult((IReadOnlyCollection<IAssetPair>) data);
        }

        public Task<IAssetAttributes> GetAssetsAttributesAsync(string assetId)
        {
            return Task.FromResult(_assetsService.AssetAttributes.GetAllForAsset(assetId));
        }

        public Task<IAssetAttribute> GetAssetAttributesAsync(string assetId, string key)
        {
            return Task.FromResult(_assetsService.AssetAttributes.Get(assetId, key));
        }

        public Task<IList<IAssetExtendedInfo>> GetAssetsExtendedInfosAsync()
        {
            var data = _assetsService.AssetExtendedInfo.GetAll();
            return Task.FromResult<IList<IAssetExtendedInfo>>(data);
        }

        public Task<IAssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId)
        {
            var data = _assetsService.AssetExtendedInfo.Get(assetId);
            return Task.FromResult(data);
        }

        public Task<IAssetExtendedInfo> GetDefaultAssetExtendedInfoAsync()
        {
            var data = _assetsService.AssetExtendedInfo.GetDefault();
            return Task.FromResult(data);
        }

        public Task<IList<IAssetCategory>> GetAssetCategoriesAsync()
        {
            var data = _assetsService.AssetCategory.GetAll();
            return Task.FromResult(data);
        }

        public Task<IAssetCategory> GetAssetCategoryAsync(string categoryId)
        {
            var data = _assetsService.AssetCategory.Get(categoryId);
            return Task.FromResult(data);
        }

        public async Task<IEnumerable<IAsset>> GetAssetsAvailableToClientAsync(
            string clientId,
            string partnerId,
            bool? tradable = default(bool?))
        {
            var userAssetIds = await _assetsServiceUserDataClient.AvailableAssets.GetAssetIds(clientId, true);

            var allAssets = await GetAllAssetsAsync();
            var relevantAssets = allAssets.Where(x => !x.IsDisabled && (!tradable.HasValue || x.IsTradable == tradable));

            var assetsAvailableToUser = new HashSet<string>(userAssetIds);

            return relevantAssets.Where(x =>
                    assetsAvailableToUser.Contains(x.Id) &&
                    (x.NotLykkeAsset
                        ? partnerId != null && x.PartnerIds.Contains(partnerId)
                        : partnerId == null || x.PartnerIds.Contains(partnerId)));
        }

        public async Task<HashSet<string>> GetSetOfAssetsAvailableToClientAsync(
            string clientId,
            string partnerId,
            bool? tradable = default(bool?))
        {
            var availableAssets = await GetAssetsAvailableToClientAsync(clientId, partnerId, tradable);
            return new HashSet<string>(availableAssets.Select(x => x.Id));
        }

        public async Task<IEnumerable<IAssetPair>> GetAssetPairsAvailableToClientAsync(string clientId, string partnerId, bool? tradable = default(bool?))
        {
            var allNondisabledAssetPairs = (await GetAllAssetPairsAsync()).Where(s => !s.IsDisabled);

            var assetsAvailableToUser = await GetSetOfAssetsAvailableToClientAsync(clientId, partnerId, tradable);

            return allNondisabledAssetPairs.Where(x =>
                    assetsAvailableToUser.Contains(x.BaseAssetId) &&
                    assetsAvailableToUser.Contains(x.QuotingAssetId));
        }

        public async Task<HashSet<string>> GetSetOfAssetPairsAvailableToClientAsync(string clientId, string partnerId, bool? tradable = default(bool?))
        {
            var availableAssetPairs = await GetAssetPairsAvailableToClientAsync(clientId, partnerId, tradable);
            return new HashSet<string>(availableAssetPairs.Select(x => x.Id));
        }

        public async Task<IWatchList> AddCustomWatchListAsync(string clientId, WatchList watchList)
        {
            var data = await _assetsServiceUserDataClient.WatchLists.AddCustomAsync(FromWatchListResponse(watchList), clientId);
            return data;
        }

        public async Task UpdateCustomWatchListAsync(string clientId, WatchListDto watchList)
        {
            await _assetsServiceUserDataClient.WatchLists.UpdateCustomWatchListAsync(clientId, watchList);
        }

        public async Task RemoveCustomWatchListAsync(string clientId, string watchListId)
        {
            await _assetsServiceUserDataClient.WatchLists.RemoveCustomAsync(watchListId, clientId);
        }

        public async Task<IList<IWatchList>> GetAllCustomWatchListsForClient(string clientId)
        {
            var data = await _assetsServiceUserDataClient.WatchLists.GetAllCustom(clientId);
            return data.ToList();
        }

        public async Task<IWatchList> GetCustomWatchListAsync(string clientId, string watchListId)
        {
            var data = await _assetsServiceUserDataClient.WatchLists.GetCustomWatchListAsync(clientId, watchListId);

            return data;
        }

        public async Task<IWatchList> GetPredefinedWatchListAsync(string watchListId)
        {
            var data = await _assetsServiceUserDataClient.WatchLists.GetPredefinedWatchListAsync(watchListId);
            return data;
        }

        private WatchListDto FromWatchListResponse(WatchList watchList)
        {
            return new WatchListDto()
            {
                Id = watchList.Id,
                Name = watchList.Name,
                Order = watchList.Order,
                ReadOnly = watchList.ReadOnlyProperty,
                AssetIds = watchList.AssetIds.ToList()
            };
        }
    }
}
