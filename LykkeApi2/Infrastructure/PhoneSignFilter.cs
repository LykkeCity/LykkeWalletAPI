using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Identity;
using Lykke.Cqrs;
using LykkeApi2.Domain.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace LykkeApi2.Infrastructure
{
    internal class PhoneSignFilter : ActionFilterAttribute
    {
        private readonly ILykkePrincipal _lykkePrincipal;
        private readonly ICqrsEngine _cqrsEngine;

        public PhoneSignFilter(ILykkePrincipal lykkePrincipal, ICqrsEngine cqrsEngine)
        {
            _lykkePrincipal = lykkePrincipal;
            _cqrsEngine = cqrsEngine;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var principal = await _lykkePrincipal.GetCurrent();

            if (principal.Claims.Where(c => c.Type == "SessionTag").All(c => c.Value != "phone"))
            {
                var commandContext = new { SessionId = _lykkePrincipal.GetToken() };

                _cqrsEngine.SendCommand(new SignCommand
                {
                    RequestId = Guid.NewGuid(),                    
                    RequestType = "PromoteSession",
                    ClientId = principal.Identity.Name,
                    Context = JsonConvert.SerializeObject(commandContext)
                }, "api", "wamp");

                context.HttpContext.Response.StatusCode = 400;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(new { Confirmation = new[] { "Method is required a confirmation using a phone" } }));                          
                return;
            }

            await next();            
        }
    }
}