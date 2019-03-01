using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using LykkeApi2.Infrastructure.ApiTrace;
using LykkeApi2.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Middleware
{
    public class ApiTraceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApiTraceSender _sender;
        private readonly ILog _log;

        public ApiTraceMiddleware(RequestDelegate next, IApiTraceSender sender, ILog log)
        {
            _next = next;
            _sender = sender;
            _log = log;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = new Stopwatch();
            Exception ex = null;
            sw.Start();
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                ex = e;
                throw;
            }
            finally
            {
                sw.Stop();

                var request = context.Request;
                var responce = context.Response;
                var body = "";

                if (!request.Path.ToString().Contains("isalive")
                    && !request.Path.ToString().Contains("wagger"))
                {
                    if (request.Method == "POST")
                    {
                        context.Request.Body.Seek(0, SeekOrigin.Begin);
                        var reader = new StreamReader(context.Request.Body);
                        body = reader.ReadToEnd();
                    }

                    var authstr = "";
                    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && auth.Any())
                    {
                        authstr = auth.First();
                    }

                    var data = new
                    {
                        DateTime = DateTime.UtcNow,
                        Level = "apitrace",
                        Path = request.Path,
                        Method = request.Method,
                        StatusCode = responce.StatusCode,
                        ExecuteTimeMs = sw.ElapsedMilliseconds,
                        AuthToken = authstr,
                        ClientId = context.User?.Identity?.Name,
                        Query = request.QueryString.ToString(),
                        Body = body,
                        Type = ex != null ? ex.GetType().Name : "",
                        Stack = ex != null ? ex.StackTrace : "",
                        Msg = ex != null ? ex.Message : "",
                    };

                    await _sender.LogMethodCall(data);

                    await _log.WriteInfoAsync("Api", "ApiTrace", data.ToJson(), "trace log");
                }
            }
        }
    }
}