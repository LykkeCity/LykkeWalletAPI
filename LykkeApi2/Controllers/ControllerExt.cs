using Common;
using LykkeApi2.Extensions;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LykkeApi2.Controllers
{
    public static class ControllerExt
    {
        public static string GetUserAgent(this Controller ctx)
        {
            return ctx.Request.GetUserAgent();
        }

        public static bool IsIosDevice(this Controller ctx)
        {
            var userAgentVariables =
                UserAgentHelper.ParseUserAgent(ctx.Request.GetUserAgent().ToLower());
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

        public static string GetIp(this Controller ctx)
        {
            string ip = string.Empty;

            // http://stackoverflow.com/a/43554000/538763
            var xForwardedForVal = GetHeaderValueAs<string>(ctx.HttpContext, "X-Forwarded-For").SplitCsv().FirstOrDefault();

            if (!string.IsNullOrEmpty(xForwardedForVal))
            {
                ip = xForwardedForVal.Split(':')[0];
            }

            // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
            if (string.IsNullOrWhiteSpace(ip) && ctx.HttpContext?.Connection?.RemoteIpAddress != null)
                ip = ctx.HttpContext.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = GetHeaderValueAs<string>(ctx.HttpContext, "REMOTE_ADDR");

            return ip;
        }

        #region Tools

        private static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
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

        private static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

            return csvList
                .TrimEnd(',')
                .Split(',')
                .AsEnumerable<string>()
                .Select(s => s.Trim())
                .ToList();
        }

        public static double? GetVersion(this Controller ctx)
        {
            var userAgentVariables =
                UserAgentHelper.ParseUserAgent(ctx.Request.GetUserAgent().ToLower());

            if (userAgentVariables.ContainsKey(UserAgentVariablesLowercase.AppVersion))
            {
                return userAgentVariables[UserAgentVariablesLowercase.AppVersion].ParseAnyDouble();
            }

            return null;
        }

        #endregion
    }
}
