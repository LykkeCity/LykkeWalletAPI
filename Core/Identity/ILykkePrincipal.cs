using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.Identity
{
    [Obsolete("Is used only for old token management")]
    public interface ILykkePrincipal
    {
        Task<ClaimsPrincipal> GetCurrent();
        void InvalidateCache(string token);
    }
}