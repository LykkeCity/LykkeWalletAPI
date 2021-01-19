using System.Collections.Generic;
using System.Threading.Tasks;
using Antares.Service.Assets.Client.Models;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Core.Domain;

namespace Core.Services
{
    public interface IAssetsHelper
    {
        Task<IAsset> GetAssetAsync(string assetId);
        Task<IReadOnlyCollection<IAsset>> GetAllAssetsAsync();
        Task<IAssetPair> GetAssetPairAsync(string assetPairId);
        Task<IReadOnlyCollection<IAssetPair>> GetAllAssetPairsAsync();
        Task<IAssetAttributes> GetAssetsAttributesAsync(string assetId);
        Task<IAssetAttribute> GetAssetAttributesAsync(string assetId, string key);
        Task<IList<IAssetExtendedInfo>> GetAssetsExtendedInfosAsync();
        Task<IAssetExtendedInfo> GetAssetExtendedInfoAsync(string assetId);
        Task<IAssetExtendedInfo> GetDefaultAssetExtendedInfoAsync();
        Task<IList<IAssetCategory>> GetAssetCategoriesAsync();
        Task<IAssetCategory> GetAssetCategoryAsync(string categoryId);

        Task<IEnumerable<IAsset>> GetAssetsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));
        Task<HashSet<string>> GetSetOfAssetsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));

        Task<IEnumerable<IAssetPair>> GetAssetPairsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));
        Task<HashSet<string>> GetSetOfAssetPairsAvailableToClientAsync(string clientId, string partnerId,
            bool? tradable = default(bool?));

        Task<IWatchList> AddCustomWatchListAsync(string clientId, WatchList watchList);
        Task UpdateCustomWatchListAsync(string clientId, WatchListDto watchList);
        Task RemoveCustomWatchListAsync(string clientId, string watchListId);
        Task<IList<IWatchList>> GetAllCustomWatchListsForClient(string clientId);
        Task<IWatchList> GetCustomWatchListAsync(string clientId, string watchListId);
        Task<IWatchList> GetPredefinedWatchListAsync(string watchListId);
    }
}
