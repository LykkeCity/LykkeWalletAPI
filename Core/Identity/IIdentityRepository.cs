using System.Threading.Tasks;

namespace Core.Identity
{
    public interface IIdentityRepository
    {
        Task<int> GenerateNewIdAsync();
    }
}