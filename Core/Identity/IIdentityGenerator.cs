using System.Threading.Tasks;

namespace Core.Identity
{
    public interface IIdentityGenerator
    {
        Task<int> GenerateNewIdAsync();
    }
}