using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Client.Models.Extensions;
using Lykke.Service.ClientAccount.Client;

namespace LkeServices
{
    public class SrvAssetsHelper
    {
        private readonly IAssetsService _assetsService;
        private readonly CachedDataDictionary<string, Asset> _cachedAssetsDictionary;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsDictionary;
        private readonly IClientAccountSettingsClient _clientAccountSettingsClient;

        public SrvAssetsHelper(
            IAssetsService assetsService,
            CachedDataDictionary<string, Asset> cachedAssetsDictionary,
            CachedDataDictionary<string, AssetPair> assetPairsDictionary,
            IClientAccountSettingsClient clientAccountSettingsClient
            )
        {
            _assetsService = assetsService;
            _cachedAssetsDictionary = cachedAssetsDictionary;
            _assetPairsDictionary = assetPairsDictionary;
            _clientAccountSettingsClient = clientAccountSettingsClient;
        }
        
        public async Task<Asset[]> GetAssetsForClient(string clientId, bool isIosDevice, string partnerId = null)
        {
            var result = await _cachedAssetsDictionary.Values();
                
            result = result.Where(x => !x.IsDisabled);

            if (partnerId != null)
            {
                return result.Where(x => x.PartnerIds != null && x.PartnerIds.Contains(partnerId)).ToArray();
            }

            var assetIdsForClient = await _assetsService.ClientGetAssetIdsAsync(clientId, isIosDevice);

            if (assetIdsForClient.Any())
            {
                result = result.Where(x => assetIdsForClient.Contains(x.Id));
            }

            return result.Where(x => !x.NotLykkeAsset).ToArray();
        }

        public async Task<string> GetBaseAssetIdForClient(string clientId, bool isIosDevice, string partnerId)
        {
            var baseAssetId = (await _clientAccountSettingsClient.GetBaseAssetAsync(clientId)).BaseAssetId;

            if (string.IsNullOrEmpty(baseAssetId))
            {
                var assetsForClient = (await GetAssetsForClient(clientId, isIosDevice, partnerId)).Where(x => x.IsBase);
                
                baseAssetId = assetsForClient.GetFirstAssetId();
            }

            return baseAssetId;
        }

        public async Task<AssetPair[]> GetAssetsPairsForClient(string clientId, bool isIosDevice, string partnerId, bool ignoreBase = false)
        {
            var assetsForClient = await GetAssetsForClient(clientId, isIosDevice, partnerId);
            var result = (await _assetPairsDictionary.Values()).Where(x => !x.IsDisabled);

            if (!ignoreBase)
                result = result.WhichHaveAssets(await GetBaseAssetIdForClient(clientId, isIosDevice, partnerId));

            return result.WhichConsistsOfAssets(assetsForClient.Select(x => x.Id).ToArray()).ToArray();
        }
    }
}