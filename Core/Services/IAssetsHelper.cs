using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;

namespace Core.Services
{
    public interface IAssetsHelper
    {
        Task<Asset> GetAssetAsync(string assetId);
        Task<IReadOnlyCollection<Asset>> GetAllAssetsAsync();
        Task<AssetPair> GetAssetPairAsync(string assetPairId);
        Task<IReadOnlyCollection<AssetPair>> GetAllAssetPairsAsync();
        Task<AssetAttributes> GetAssetsAttributesAsync(string assetId);
        Task<AssetAttribute> GetAssetAttributesAsync(string assetId, string key);
        Task<IList<AssetExtendedInfo>> GetAssetsExtendedInfosAsync();
        Task<AssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId);
        Task<AssetExtendedInfo> GetDefaultAssetExtendedInfoAsync();
        Task<IList<AssetCategory>> GetAssetCategoriesAsync();
        Task<AssetCategory> GetAssetCategoryAsync(string categoryId);
        Task<HashSet<string>> GetAssetsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));
        Task<HashSet<string>> GetAssetPairsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));

        Task<WatchList> AddCustomWatchListAsync(string clientId, WatchList watchList);
        Task UpdateCustomWatchListAsync(string clientId, WatchList watchList);
        Task RemoveCustomWatchListAsync(string clientId, string watchListId);
        Task<IList<WatchList>> GetAllCustomWatchListsForClient(string clientId);
        Task<WatchList> GetCustomWatchListAsync(string clientId, string watchListId);
        Task<WatchList> GetPredefinedWatchListAsync(string watchListId);
    }
}