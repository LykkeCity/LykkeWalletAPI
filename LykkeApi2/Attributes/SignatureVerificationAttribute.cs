using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LykkeApi2.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    sealed public class SignatureVerificationAttribute : ActionFilterAttribute
    {
        public const string AccessTokenHeaderName = "SignatureVerificationToken";

        public SignatureVerificationAttribute()
        {
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var accessTokenHeader = context.HttpContext.Request.Headers[AccessTokenHeaderName];
            var headerValue = accessTokenHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(headerValue))
            {
                SetError(context, "Temp access token is not provided");

                return;
            }
            
            // security logic here

            await next();
        }

        private void SetError(ActionExecutingContext context, string error)
        {
            context.HttpContext.Response.StatusCode = 400;
            context.Result = new BadRequestResult();
        }
    }
}