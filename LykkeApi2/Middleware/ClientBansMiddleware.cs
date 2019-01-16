using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Middleware
{
    public class ClientBansMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILog _log;

        public ClientBansMiddleware(RequestDelegate next, IClientAccountClient clientAccountClient,
            ILog log)
        {
            _clientAccountClient = clientAccountClient;
            _log = log;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            bool clientBanned = false;
            string clientId = string.Empty;
            try
            {

                clientId = context.User?.Identity?.Name;

                if (!string.IsNullOrEmpty(clientId))
                {
                    clientBanned = await _clientAccountClient.IsClientBannedAsync(clientId);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(ClientBansMiddleware), clientId, ex);
            }
            finally
            {
                if (!clientBanned)
                    await _next.Invoke(context);
                else
                    context.Response.StatusCode = 403;
            }
        }
    }
}
