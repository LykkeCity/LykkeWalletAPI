using System.Linq;
using LykkeApi2.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Infrastructure
{
    public interface IRequestContext
    {
        string GetIp();
        string GetUserAgent();
    }

    public class RequestContext : IRequestContext
    {
        private readonly HttpContext _httpContext;

        public RequestContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        public string GetIp()
        {
            string ip = string.Empty;

            // http://stackoverflow.com/a/43554000/538763
            var xForwardedForVal = _httpContext.GetHeaderValueAs<string>("X-Forwarded-For").SplitCsv().FirstOrDefault();

            if (!string.IsNullOrEmpty(xForwardedForVal))
            {
                ip = xForwardedForVal.Split(':')[0];
            }

            // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
            if (string.IsNullOrWhiteSpace(ip) && _httpContext.Connection?.RemoteIpAddress != null)
                ip = _httpContext.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = _httpContext.GetHeaderValueAs<string>("REMOTE_ADDR");

            return ip;
        }

        public string GetUserAgent()
        {
            return _httpContext.Request.GetUserAgent();
        }
    }
}