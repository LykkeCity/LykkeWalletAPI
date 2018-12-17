using System;
using System.Linq;
using System.Security.Claims;
using Common;
using Core.Constants;
using Lykke.Common.ApiLibrary.Authentication;
using LykkeApi2.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Infrastructure
{
    public interface IRequestContext
    {
        string GetIp();
        string UserAgent { get; }
        string ClientId { get; }
        string PartnerId { get; }
        bool IsIosDevice { get; }
        double? Version { get; }    
        string SessionId { get; }
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

        public string UserAgent => _httpContext.Request.GetUserAgent();

        public string ClientId
        {
            get
            {
                //todo: @mkobzev. Switch to async/await
                var lykkeUser = _httpContext.AuthenticateAsync(AuthentcationSchemes.Bearer).Result;

                if (lykkeUser != null)
                {
                    return _httpContext.User?.Identity?.Name;
                }

                var ironCladUser = _httpContext.AuthenticateAsync(AuthentcationSchemes.Ironclad).Result;

                var lsub = ironCladUser.Principal.Claims.FirstOrDefault(x => x.Type == "lsub")?.Value;

                return lsub;
            }
        }

        public string PartnerId
        {
            get
            {
                var identity = (ClaimsIdentity) _httpContext?.User.Identity;
                return identity?.Claims.FirstOrDefault(x => x.Type == LykkeConstants.PartnerId)?.Value;
            }
        }

        public string SessionId
        {
            get
            {
                var identity = (ClaimsIdentity) _httpContext?.User.Identity;
                var sessionIdFromIdentity = identity?.Claims.FirstOrDefault(x => x.Type == LykkeConstants.SessionId);

                return sessionIdFromIdentity != null ? sessionIdFromIdentity.Value : GetOldLykkeToken();
            }
        }

        private string GetOldLykkeToken()
        {
            var header = _httpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(header))
                return null;

            var values = header.Split(' ');

            if (values.Length != 2)
                return null;

            if (values[0] != "Bearer")
                return null;

            return values[1];
        }

        public bool IsIosDevice
        {
            get
            {
                var userAgentVariables =
                    UserAgentHelper.ParseUserAgent(_httpContext.Request.GetUserAgent().ToLower());
                if (userAgentVariables.ContainsKey(UserAgentVariablesLowercase.DeviceType))
                {
                    if (userAgentVariables[UserAgentVariablesLowercase.DeviceType] == DeviceTypesLowercase.IPad ||
                        userAgentVariables[UserAgentVariablesLowercase.DeviceType] == DeviceTypesLowercase.IPhone)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public double? Version
        {
            get
            {
                var userAgentVariables =
                    UserAgentHelper.ParseUserAgent(_httpContext.Request.GetUserAgent().ToLower());

                if (userAgentVariables.ContainsKey(UserAgentVariablesLowercase.AppVersion))
                {
                    return userAgentVariables[UserAgentVariablesLowercase.AppVersion].ParseAnyDouble();
                }

                return null;
            }
        }
    }
}