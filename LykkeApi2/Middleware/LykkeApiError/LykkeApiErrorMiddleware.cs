using System.Threading.Tasks;
using Core.Exceptions;
using JetBrains.Annotations;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace LykkeApi2.Middleware.LykkeApiError
{
    [UsedImplicitly]
    internal class LykkeApiErrorMiddleware
    {
        private readonly RequestDelegate _next;

        public LykkeApiErrorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (LykkeApiErrorException ex)
            {
                await CreateErrorResponse(context, ex);
            }
        }

        private async Task CreateErrorResponse(HttpContext ctx, LykkeApiErrorException ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int) ex.StatusCode;
            await ctx.Response.WriteAsync(JsonConvert.SerializeObject(
                new LykkeApiErrorResponse
                {
                    Error = ex.LykkeApiErrorCode.Name,
                    Message = ex.Message
                }));
        }
    }
}