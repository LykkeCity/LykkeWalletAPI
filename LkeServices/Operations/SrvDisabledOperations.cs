using System.Threading.Tasks;
using Common;
using Core;
using Core.Constants;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Settings.Client;

namespace LkeServices.Operations
{
    public class SrvDisabledOperations
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetsPairsDict;
        private readonly CachedTradableAssetsDictionary _tradableAssetsDict;
        private readonly ISettingsClient _settingsService;

        public SrvDisabledOperations(
            CachedDataDictionary<string, AssetPair> assetsPairsDict,
            CachedTradableAssetsDictionary tradableAssetsDict,
            ISettingsClient settingsService)
        {
            _assetsPairsDict = assetsPairsDict;
            _tradableAssetsDict = tradableAssetsDict;
            _settingsService = settingsService;
        }

        public async Task<bool> IsOperationForAssetDisabled(string assetId)
        {
            var settings = await _settingsService.GetAppGlobalSettingsAsync();
            bool btcBlockchainOpsDisabled = settings.BitcoinBlockchainOperationsDisabled.GetValueOrDefault();
            bool btcOnlyDisabled = settings.BtcOperationsDisabled.GetValueOrDefault();

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