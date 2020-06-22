using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using LykkeApi2.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Middleware
{
    public class CheckSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SessionCheckSettings _sessionCheckSettings;
        private readonly ILog _log;
        private readonly string[] _checkMethods;

        public CheckSessionMiddleware(
            RequestDelegate next,
            SessionCheckSettings sessionCheckSettings,
            ILog log
        )
        {
            _next = next;
            _sessionCheckSettings = sessionCheckSettings;
            _log = log;
            _checkMethods = new[] {"POST", "PUT", "DELETE"};
        }

        public async Task Invoke(HttpContext context)
        {
            bool sessionConfirmed = true;
            string clientId = string.Empty;

            try
            {
                clientId = context.User?.Identity?.Name;

                if (_checkMethods.Contains(context.Request.Method, StringComparer.InvariantCultureIgnoreCase) &&
                    !_sessionCheckSettings.SkipPaths.Any(x => context.Request.Path.StartsWithSegments(x, StringComparison.InvariantCultureIgnoreCase)))
                {
                    sessionConfirmed = context.User?.Identity?.IsSessionConfirmed() ?? false;
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CheckSessionMiddleware), clientId, ex);
            }
            finally
            {
                if (sessionConfirmed)
                    await _next.Invoke(context);
                else
                    context.Response.StatusCode = 403;
            }
        }
    }
}
