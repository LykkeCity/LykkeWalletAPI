using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Constants;
using Core.Identity;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;

namespace LkeServices.Identity
{
    public class LykkePrincipal : ILykkePrincipal
    {
        private readonly ClaimsCache _claimsCache = new ClaimsCache();
        private readonly IClientSessionsClient _clientSessionsClient;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public LykkePrincipal(IHttpContextAccessor httpContextAccessor, IClientSessionsClient clientSessionsClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientSessionsClient = clientSessionsClient;
        }

        public string GetToken()
        {
            var context = _httpContextAccessor.HttpContext;

            var header = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(header))
                return null;

            var values = header.Split(' ');

            if (values.Length != 2)
                return null;

            if (values[0] != "Bearer")
                return null;

            return values[1];
        }

        public void InvalidateCache(string token)
        {
            _claimsCache.Invalidate(token);
        }

        public async Task SetSessionConfirmedAsync()
        {
            var result = await GetCurrent();

            if (result != null)
            {
                var claim = result.Claims.FirstOrDefault(x => x.Type == LykkeConstants.SessionConfirmed);
                var token = result.Claims.FirstOrDefault(x => x.Type == LykkeConstants.SessionId);

                if (claim != null)
                {
                    ((ClaimsIdentity)result.Identity).RemoveClaim(claim);
                }

                ((ClaimsIdentity)result.Identity).AddClaim(new Claim(LykkeConstants.SessionConfirmed, true.ToString()));

                if (token != null)
                    _claimsCache.Set(token.Value, result);
            }
        }

        public async Task<ClaimsPrincipal> GetCurrent()
        {
            var token = GetToken();

            if (string.IsNullOrWhiteSpace(token))
                return null;

            var result = _claimsCache.Get(token);
            if (result != null)
                return result;

            var session = await _clientSessionsClient.GetAsync(token);
            if (session == null)
                return null;

            result = new ClaimsPrincipal(LykkeIdentity.Create(session.ClientId));
            if (session.PartnerId != null)
            {
                (result.Identity as ClaimsIdentity)?.AddClaim(new Claim(LykkeConstants.PartnerId, session.PartnerId));
            }

            if (session.Pinned)
            {
                (result.Identity as ClaimsIdentity)?.AddClaim(new Claim("TokenType", "Pinned"));
            }

            (result.Identity as ClaimsIdentity)?.AddClaim(new Claim(LykkeConstants.SessionId, session.SessionToken));
            (result.Identity as ClaimsIdentity)?.AddClaim(new Claim(LykkeConstants.SessionConfirmed, session.IsSessionConfirmed.ToString()));

            _claimsCache.Set(token, result);
            return result;
        }
    }
}
