using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;

namespace Core.Services
{
    public interface IAssetsHelper
    {
        Task<Asset> GetAssetAsync(string assetId);
        Task<IEnumerable<Asset>> GetAllAssetsAsync();
        Task<AssetPair> GetAssetPairAsync(string assetPairId);
        Task<IEnumerable<AssetPair>> GetAllAssetPairsAsync();
        Task<AssetAttributes> GetAssetsAttributesAsync(string assetId);
        Task<AssetAttribute> GetAssetAttributesAsync(string assetId, string key);
        Task<IEnumerable<AssetExtendedInfo>> GetAssetsExtendedInfosAsync();
        Task<AssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId);
        Task<AssetExtendedInfo> GetDefaultAssetExtendedInfoAsync();
        Task<IEnumerable<AssetCategory>> GetAssetCategoriesAsync();
        Task<AssetCategory> GetAssetCategoryAsync(string categoryId);
        Task<HashSet<string>> GetAssetsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));

        Task<WatchList> AddCustomWatchListAsync(string clientId, WatchList watchList);
        Task UpdateCustomWatchListAsync(string clientId, WatchList watchList);
        Task RemoveCustomWatchListAsync(string clientId, string watchListId);
        Task<IEnumerable<WatchList>> GetAllCustomWatchListsForClient(string clientId);
        Task<WatchList> GetCustomWatchListAsync(string clientId, string watchListId);
        Task<WatchList> GetPredefinedWatchListAsync(string watchListId);
    }
}