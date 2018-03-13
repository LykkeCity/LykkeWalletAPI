using System.Linq;
using System.Threading.Tasks;
using Core.Clients;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;

namespace LkeServices.Clients
{
    public class SrvBackup : ISrvBackup
    {
        private readonly IClientAccountClient _clientAccountService;
        private readonly IBalancesClient _balancesClient;

        public SrvBackup(IClientAccountClient clientAccountService,
            IBalancesClient balancesClient)
        {
            _clientAccountService = clientAccountService;
            _balancesClient = balancesClient;
        }

        public async Task<bool> IsBackupRequired(string clientId)
        {
            var backupSettingsTask = _clientAccountService.GetBackupAsync(clientId);
            var wallets = await _balancesClient.GetClientBalances(clientId);
            return wallets.Any(x => x.Balance > 0) && !(await backupSettingsTask).BackupDone;
        }

        public async Task<bool> IsBackupRequiredWithoutWalletCheck(string clientId)
        {
            var backupSettingsTask = _clientAccountService.GetBackupAsync(clientId);
            return !(await backupSettingsTask).BackupDone;
        }
    }
}