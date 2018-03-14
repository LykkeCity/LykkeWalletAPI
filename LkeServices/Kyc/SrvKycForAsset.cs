using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Clients;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;

namespace LkeServices.Kyc
{
    public class SrvKycForAsset
    {
        private readonly CachedTradableAssetsDictionary _tradableAssets;
        private readonly IKycStatusService _kycStatusService;
        private readonly ISkipKycRepository _skipKycRepository;

        public SrvKycForAsset(
            CachedTradableAssetsDictionary tradableAssets,
            IKycStatusService kycStatusService,
            ISkipKycRepository skipKycRepository)
        {
            _tradableAssets = tradableAssets;
            _kycStatusService = kycStatusService;
            _skipKycRepository = skipKycRepository;
        }

        public async Task<bool> IsKycNeeded(string clientId, string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentException(nameof(assetId));

            var asset = await _tradableAssets.GetItemAsync(assetId);

            if (asset == null)
                throw new ArgumentException(nameof(assetId));

            var userKycStatus = await _kycStatusService.GetKycStatusAsync(clientId);

            return asset.KycNeeded && !userKycStatus.IsKycOkOrReviewDone();
        }

        public async Task<bool?> CanSkipKyc(string clientId, string assetId, AssetPair assetPair, decimal volume)
        {
            var allowedAssets = new[] { "BTC", "ETH", "SLR", "TIME" };
            var canSkipKyc = await _skipKycRepository.CanSkipKyc(clientId);

            if (!canSkipKyc)
                return null;

            return volume > 0 && allowedAssets.Contains(assetId, StringComparer.InvariantCultureIgnoreCase) ||
                   volume < 0 && !allowedAssets.Contains(assetId, StringComparer.InvariantCultureIgnoreCase) &&
                   (allowedAssets.Contains(assetPair.BaseAssetId, StringComparer.InvariantCultureIgnoreCase) || allowedAssets.Contains(assetPair.QuotingAssetId, StringComparer.InvariantCultureIgnoreCase));
        }
    }
}