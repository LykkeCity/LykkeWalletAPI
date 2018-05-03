using System.Threading.Tasks;
using Core.Domain.BitCoin;

namespace Core.Services
{
    public interface IWalletCredentialsService
    {
        Task<IWalletCredentials> GetAsync(string clientId);
    }
}