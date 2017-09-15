using System.Collections.Generic;
using System.Linq;

namespace LykkeApi2.Infrastructure
{
    public static class UserAgentHelper
    {
        public static IDictionary<string, string> ParseUserAgent(string userAgent)
        {
            if (!string.IsNullOrEmpty(userAgent))
                return userAgent.Split(';').Select(parameter => parameter.Split('='))
                    .Where(x => x.Length == 2)
                    .GroupBy(x => x[0])
                    .ToDictionary(x => x.Key, x => x.First()[1]);
            return new Dictionary<string, string>();
        }
    }
}
