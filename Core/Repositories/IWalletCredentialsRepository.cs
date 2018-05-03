using System.Threading.Tasks;
using Core.Domain.BitCoin;

namespace Core.Repositories
{
    public interface IWalletCredentialsRepository
    {
        Task<IWalletCredentials> GetAsync(string clientId);
    }
}