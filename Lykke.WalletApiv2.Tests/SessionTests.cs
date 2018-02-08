using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Service.Session.Client;
using Newtonsoft.Json;
using Xunit;
using IClientSessionsClient = Lykke.Service.Session.Client.IClientSessionsClient;

namespace Lykke.WalletApiv2.Tests
{
    public class SessionTests
    {
        [Fact(Skip = "integration test")]
        public async Task RedisTest()
        {
            var builder = new ContainerBuilder();

            builder.RegisterRedisClientSession(
                new SessionsSettings
                {
                    RedisCluster = new[] {"redis-master.lykke-sessions.svc.cluster.local:6379"},
                    SessionIdleTimeout = TimeSpan.FromDays(3),
                    PhoneKeyTtl = TimeSpan.FromMinutes(5)
                });

            var container = builder.Build();
            var client = container.Resolve<IClientSessionsClient>();

            var result = await client.Authenticate("27fe9c28-a18b-4939-8ebf-a70061fbfa05", null);
            await client.SetTag(result.SessionToken, "phone");
            result = await client.GetAsync(result.SessionToken);
            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }
}