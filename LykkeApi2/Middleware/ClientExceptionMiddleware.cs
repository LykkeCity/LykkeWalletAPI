using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace LykkeApi2.Middleware
{
    [UsedImplicitly]
    public class ClientExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CreateErrorResponse _createErrorResponse;
        
        public ClientExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ClientException ex)
            {
                await CreateErrorResponse(context, ex);
            }
        }

        private async Task CreateErrorResponse(HttpContext ctx, ClientException ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)ex.StatusCode;
            await ctx.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                error = ex.ExceptionType.ToString(), 
                message = ClientException.GetTextForException(ex.ExceptionType)
            }));
        }
    }
}