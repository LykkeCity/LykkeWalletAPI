using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LykkeApi2.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Middleware
{
    public class ApiTraceMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiTraceMiddleware(RequestDelegate next)
        {
            _next = next;
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
                    && !request.Path.ToString().Contains("Swagger"))
                {
                    if (request.Method == "POST")
                    {
                        context.Request.Body.Seek(0, SeekOrigin.Begin);
                        var reader = new StreamReader(context.Request.Body);
                        body = reader.ReadToEnd();
                    }

                    responce.Body.Seek(0, SeekOrigin.Begin);
                    var readerResp = new StreamReader(context.Request.Body);
                    var bodyResp = readerResp.ReadToEnd();
                    responce.Body.Seek(0, SeekOrigin.Begin);

                    var authstr = "";
                    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && auth.Any())
                    {
                        authstr = auth.First();
                    }

                    Console.WriteLine("--------------------------");
                    Console.WriteLine($"Path: {request.Path}");
                    Console.WriteLine($"Method: {request.Method}");
                    Console.WriteLine($"StatusCode: {responce.StatusCode}");
                    Console.WriteLine($"ExecuteTime: {sw.ElapsedMilliseconds} ms");
                    Console.WriteLine($"ClientId: {context.User?.Identity?.Name}");
                    Console.WriteLine($"AuthToken: {authstr}");
                    if (ex != null)
                    {
                        Console.WriteLine("==============");
                        Console.WriteLine(ex);
                        Console.WriteLine("==============");
                    }

                    Console.WriteLine($"Body: {body}");
                    Console.WriteLine($"ResponceBody: {bodyResp.Substring(0, 30)}");
                }
            }
        }
    }
}