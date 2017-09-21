using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace LykkeApi2.App_Start
{
    public static class Dependencies
    {
        public static Func<object, string> GetIdentity;
        public static Func<object, string> GetPartnerId;

        static Dependencies()
        {
            GetIdentity = ctr =>
            {
                var ctx = ctr as Controller;
                return ctx?.User?.Identity.Name;
            };

            GetPartnerId = ctr =>
            {
                var ctx = ctr as Controller;
                var identity = (ClaimsIdentity)ctx?.User.Identity;
                return identity?.Claims.FirstOrDefault(x => x.Type == "PartnerId")?.Value;
            };
        }
    }
}
