using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Services;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;

namespace LkeServices
{
    [UsedImplicitly]
    public class AssetsHelper : IAssetsHelper
    {
        private readonly IAssetsService _assetsService;
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsCache;

        public AssetsHelper(
            IAssetsService assetsService,
            CachedDataDictionary<string, Asset> assetsCache,
            CachedDataDictionary<string, AssetPair> assetPairsCache)
        {
            _assetsService = assetsService;
            _assetsCache = assetsCache;
            _assetPairsCache = assetPairsCache;
        }

        public Task<Asset> GetAssetAsync(string assetId)
        {
            return _assetsCache.GetItemAsync(assetId);
        }

        public Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            return _assetsCache.Values();
        }

        public Task<AssetPair> GetAssetPairAsync(string assetPairId)
        {
            return _assetPairsCache.GetItemAsync(assetPairId);
        }

        public Task<IEnumerable<AssetPair>> GetAllAssetPairsAsync()
        {
            return _assetPairsCache.Values();
        }

        public Task<AssetAttributes> GetAssetsAttributesAsync(string assetId)
        {
            return _assetsService.AssetAttributeGetAllForAssetAsync(assetId);
        }

        public Task<AssetAttribute> GetAssetAttributesAsync(string assetId, string key)
        {
            return _assetsService.AssetAttributeGetAsync(assetId, key);
        }

        public async Task<IEnumerable<AssetExtendedInfo>> GetAssetsExtendedInfosAsync()
        {
            var resp = await _assetsService.AssetExtendedInfoGetAllAsync();
            return resp;
        }

        public Task<AssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId)
        {
            return _assetsService.AssetExtendedInfoGetAsync(assetId);
        }

        public Task<AssetExtendedInfo> GetDefaultAssetExtendedInfoAsync()
        {
            return _assetsService.AssetExtendedInfoGetDefaultAsync();
        }

        public async Task<IEnumerable<AssetCategory>> GetAssetCategoriesAsync()
        {
            var resp = await _assetsService.AssetCategoryGetAllAsync();
            return resp;
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
            var allAssets = await _assetsCache.Values();
            var relevantAssets = allAssets.Where(x => !x.IsDisabled && (!tradable.HasValue || x.IsTradable == tradable ));
            
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

        public async Task<IEnumerable<WatchList>> GetAllCustomWatchListsForClient(string clientId)
        {
            var resp = await _assetsService.WatchListGetAllAsync(clientId);
            return resp;
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