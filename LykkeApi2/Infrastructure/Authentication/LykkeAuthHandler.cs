using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Core.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LykkeApi2.Infrastructure.Authentication
{
    public class LykkeAuthHandler : AuthenticationHandler<LykkeAuthOptions>
    {
        private readonly ILykkePrincipal _lykkePrincipal;

        public LykkeAuthHandler(IOptionsMonitor<LykkeAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ILykkePrincipal lykkePrincipal) : base(options, logger, encoder, clock)
        {
            _lykkePrincipal = lykkePrincipal;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = await _lykkePrincipal.GetCurrent();

            if (principal == null)
                return AuthenticateResult.NoResult();

            var ticket = new AuthenticationTicket(principal, "Bearer");
            
            return AuthenticateResult.Success(ticket);
        }
    }
}