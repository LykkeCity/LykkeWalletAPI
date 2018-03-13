using System.Threading.Tasks;

namespace Core.Clients
{
    public interface ISrvBackup
    {
        Task<bool> IsBackupRequired(string clientId);
        Task<bool> IsBackupRequiredWithoutWalletCheck(string clientId);
    }
}