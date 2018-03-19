using System.Threading.Tasks;
using Core.Identity;
using Lykke.Service.Session.Client;
using Lykke.Service.Session.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace LykkeApi2.Infrastructure
{
    internal class PhoneSignFilter : ActionFilterAttribute
    {
        private readonly ILykkePrincipal _lykkePrincipal;        
        private readonly IClientSessionsClient _clientSessionsClient;

        public PhoneSignFilter(ILykkePrincipal lykkePrincipal, IClientSessionsClient clientSessionsClient)
        {
            _lykkePrincipal = lykkePrincipal;            
            _clientSessionsClient = clientSessionsClient;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var result = await _clientSessionsClient.ValidateAsync(_lykkePrincipal.GetToken(), RequestType.Orders);

            if (!result)
            {
                context.HttpContext.Response.StatusCode = 400;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(new { Confirmation = new[] { "Method is required a confirmation using a phone" } }));                          
                return;
            }

            await next();            
        }
    }
}