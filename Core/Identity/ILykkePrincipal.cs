using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.Identity
{
    public interface ILykkePrincipal
    {
        Task<ClaimsPrincipal> GetCurrent();
        string GetToken();
        void InvalidateCache(string token);
    }
}