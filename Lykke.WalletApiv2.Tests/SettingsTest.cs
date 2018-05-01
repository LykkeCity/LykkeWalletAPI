using System.Diagnostics;
using System.Net;
using Core;
using Newtonsoft.Json;
using Xunit;

namespace Lykke.WalletApiv2.Tests
{
    public class SettingsTest
    {
        [Fact]
        public void Cleanup()
        {
            var data = new WebClient().DownloadString("http://settings.lykke-settings.svc.cluster.local/rr5999apiv2999dvgsert25uwheifn_WalletApiv2");

            var settings = JsonConvert.DeserializeObject<APIv2Settings>(data);

            var result = JsonConvert.SerializeObject(settings, Formatting.Indented);

            Trace.WriteLine(result);
        }
    }
}