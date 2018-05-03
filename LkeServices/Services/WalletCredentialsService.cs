using System.Threading.Tasks;
using Core.Domain.BitCoin;
using Core.Repositories;
using Core.Services;

namespace LkeServices
{
    public class WalletCredentialsService: IWalletCredentialsService
    {
        private readonly IWalletCredentialsRepository _walletCredentialsRepository;

        public WalletCredentialsService(IWalletCredentialsRepository walletCredentialsRepository)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
        }

        public async Task<IWalletCredentials> GetAsync(string clientId)
        {
            return await _walletCredentialsRepository.GetAsync(clientId);
        }
    }
}