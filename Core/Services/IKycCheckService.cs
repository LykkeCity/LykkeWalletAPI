using System.Threading.Tasks;

namespace Core.Services
{
    public interface IKycCheckService
    {
        Task<bool> IsKycNeededAsync(string clientId);
    }
}
