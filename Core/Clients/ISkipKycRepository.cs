using System.Threading.Tasks;

namespace Core.Clients
{
    public interface ISkipKycRepository
    {
        Task<bool> CanSkipKyc(string clientId);
        Task SkipKyc(string clientId, bool skip);
    }
}