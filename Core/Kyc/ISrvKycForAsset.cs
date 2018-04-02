using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;

namespace Core.Kyc
{
    public interface ISrvKycForAsset
    {
        Task<bool> IsKycNeeded(string clientId, string assetId);
        Task<bool?> CanSkipKyc(string clientId, string assetId, AssetPair assetPair, decimal volume);
    }
}