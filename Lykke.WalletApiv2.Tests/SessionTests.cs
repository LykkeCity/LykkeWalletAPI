using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Service.Session.Client;
using Lykke.Service.Session.Contracts;
using Newtonsoft.Json;
using Xunit;
using IClientSessionsClient = Lykke.Service.Session.Client.IClientSessionsClient;

namespace Lykke.WalletApiv2.Tests
{
    public class SessionTests
    {
        [Fact()]
        public async Task RedisTest()
        {
            var builder = new ContainerBuilder();

            builder.RegisterClientSessionClient("http://session.lykke-service.svc.cluster.local", new LogToConsole());

            var container = builder.Build();
            var client = container.Resolve<IClientSessionsClient>();

            var result = await client.ValidateAsync("3d454daec12f4b40b804d1f05db0b3e237933ff741654bc7bd26ee0a8cfa891a", RequestType.Orders);
            
            Assert.True(result);

            Trace.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
    }
}