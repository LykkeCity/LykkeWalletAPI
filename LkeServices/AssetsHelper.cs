using Core.Services;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices
{
    [UsedImplicitly]
    public class AssetsHelper : IAssetsHelper
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public AssetsHelper(
            IAssetsService assetsService, IAssetsServiceWithCache assetsServiceWithCache)
        {
            _assetsService = assetsService;
            _assetsServiceWithCache = assetsServiceWithCache;
        }

        public Task<Asset> GetAssetAsync(string assetId)
        {
            return _assetsServiceWithCache.TryGetAssetAsync(assetId);
        }

        public Task<IReadOnlyCollection<Asset>> GetAllAssetsAsync()
        {
            return _assetsServiceWithCache.GetAllAssetsAsync(true);
        }

        public Task<AssetPair> GetAssetPairAsync(string assetPairId)
        {
            return _assetsServiceWithCache.TryGetAssetPairAsync(assetPairId);
        }

        public Task<IReadOnlyCollection<AssetPair>> GetAllAssetPairsAsync()
        {
            return _assetsServiceWithCache.GetAllAssetPairsAsync();
        }

        public Task<AssetAttributes> GetAssetsAttributesAsync(string assetId)
        {
            return _assetsService.AssetAttributeGetAllForAssetAsync(assetId);
        }

        public Task<AssetAttribute> GetAssetAttributesAsync(string assetId, string key)
        {
            return _assetsService.AssetAttributeGetAsync(assetId, key);
        }

        public Task<IList<AssetExtendedInfo>> GetAssetsExtendedInfosAsync()
        {
            return _assetsService.AssetExtendedInfoGetAllAsync();
        }

        public Task<AssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId)
        {
            return _assetsService.AssetExtendedInfoGetAsync(assetId);
        }

        public Task<AssetExtendedInfo> GetDefaultAssetExtendedInfoAsync()
        {
            return _assetsService.AssetExtendedInfoGetDefaultAsync();
        }

        public Task<IList<AssetCategory>> GetAssetCategoriesAsync()
        {
            return _assetsService.AssetCategoryGetAllAsync();
        }

        public Task<AssetCategory> GetAssetCategoryAsync(string categoryId)
        {
            return _assetsService.AssetCategoryGetAsync(categoryId);
        }

        public async Task<HashSet<string>> GetAssetsAvailableToClientAsync(
            string clientId,
            string partnerId,
            bool? tradable = default(bool?))
        {
            var allAssets = await GetAllAssetsAsync();
            var relevantAssets = allAssets.Where(x => !x.IsDisabled && (!tradable.HasValue || x.IsTradable == tradable));

            var assetsAvailableToUser = new HashSet<string>(await _assetsService.ClientGetAssetIdsAsync(clientId, true));

            var result = new HashSet<string>(relevantAssets.Where(x =>
                    assetsAvailableToUser.Contains(x.Id) &&
                    (x.NotLykkeAsset
                        ? partnerId != null && x.PartnerIds.Contains(partnerId)
                        : partnerId == null || x.PartnerIds.Contains(partnerId)))
                .Select(x => x.Id));

            return result;
        }

        public async Task<HashSet<string>> GetAssetPairsAvailableToClientAsync(string clientId, string partnerId, bool? tradable = default(bool?))
        {
            var allNondisabledAssetPairs = (await GetAllAssetPairsAsync()).Where(s => !s.IsDisabled);

            var assetsAvailableToUser = await GetAssetsAvailableToClientAsync(clientId, partnerId, tradable);

            var availableAssetPairs =
                allNondisabledAssetPairs.Where(x =>
                    assetsAvailableToUser.Contains(x.BaseAssetId) &&
                    assetsAvailableToUser.Contains(x.QuotingAssetId));

            return new HashSet<string>(availableAssetPairs.Select(x => x.Id));
        }

        public Task<WatchList> AddCustomWatchListAsync(string clientId, WatchList watchList)
        {
            return _assetsService.WatchListAddCustomAsync(watchList, clientId);
        }

        public Task UpdateCustomWatchListAsync(string clientId, WatchList watchList)
        {
            return _assetsService.WatchListUpdateCustomAsync(watchList, clientId);
        }

        public Task RemoveCustomWatchListAsync(string clientId, string watchListId)
        {
            return _assetsService.WatchListCustomRemoveAsync(watchListId, clientId);
        }

        public Task<IList<WatchList>> GetAllCustomWatchListsForClient(string clientId)
        {
            return _assetsService.WatchListGetAllAsync(clientId);
        }

        public Task<WatchList> GetCustomWatchListAsync(string clientId, string watchListId)
        {
            return _assetsService.WatchListGetCustomAsync(watchListId, clientId);
        }

        public Task<WatchList> GetPredefinedWatchListAsync(string watchListId)
        {
            return _assetsService.WatchListGetPredefinedAsync(watchListId);
        }
    }
}