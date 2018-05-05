using System.Threading.Tasks;
using Common;
using Core;
using Core.Constants;
using Core.Services;
using Lykke.Service.Assets.Client.Models;

namespace LkeServices.Operations
{
    public class SrvDisabledOperations
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetsPairsDict;
        private readonly CachedTradableAssetsDictionary _tradableAssetsDict;
        private readonly ISettingsService _settingsService;

        public SrvDisabledOperations(
            CachedDataDictionary<string, AssetPair> assetsPairsDict,
            CachedTradableAssetsDictionary tradableAssetsDict,
            ISettingsService settingsService)
        {
            _assetsPairsDict = assetsPairsDict;
            _tradableAssetsDict = tradableAssetsDict;
            _settingsService = settingsService;
        }

        public async Task<bool> IsOperationForAssetDisabled(string assetId)
        {
            var settings = await _settingsService.GetAppGlobalSettingsSettingsAsync();
            bool btcBlockchainOpsDisabled = settings.BitcoinBlockchainOperationsDisabled;
            bool btcOnlyDisabled = settings.BtcOperationsDisabled;

            return btcOnlyDisabled && assetId == LykkeConstants.BitcoinAssetId ||
                   btcBlockchainOpsDisabled && await IsBtcBlockchainAsset(assetId);
        }

        public async Task<bool> IsOperationForAssetPairDisabled(string assetPairId)
        {
            var pair = await _assetsPairsDict.GetItemAsync(assetPairId);
            return await IsOperationForAssetDisabled(pair.BaseAssetId) ||
                   await IsOperationForAssetDisabled(pair.QuotingAssetId);
        }

        private async Task<bool> IsBtcBlockchainAsset(string assetId)
        {
            var asset = await _tradableAssetsDict.GetItemAsync(assetId);
            return asset.Blockchain == Blockchain.Bitcoin;
        }
    }
}