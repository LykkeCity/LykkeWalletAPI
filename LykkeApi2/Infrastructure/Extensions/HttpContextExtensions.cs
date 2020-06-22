using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Core.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LykkeApi2.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        public static Uri GetUri(this HttpRequest request)
        {
            var hostComponents = request.Host.ToUriComponent().Split(':');

            var builder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = hostComponents[0],
                Path = request.Path,
                Query = request.QueryString.ToUriComponent()
            };

            if (hostComponents.Length == 2)
            {
                builder.Port = Convert.ToInt32(hostComponents[1]);
            }

            return builder.Uri;
        }

        public static string GetUserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"].ToString();
        }

        public static T GetHeaderValueAs<T>(this HttpContext httpContext, string headerName)
        {
            StringValues values;

            if (httpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!string.IsNullOrEmpty(rawValues))
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default(T);
        }

        public static bool IsSessionConfirmed(this IIdentity identity)
        {
            bool.TryParse(((ClaimsIdentity)identity)?.Claims.FirstOrDefault(c => c.Type == LykkeConstants.SessionConfirmed)?.Value, out var sessionConfirmed);
            return sessionConfirmed;
        }
    }
}
