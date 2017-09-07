using AzureStorage.Blob;
using Common;
using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace LkeServices
{
    public static class GeneralSettingsReader
    {
        [Obsolete("Please, migrate to settings service")]
        public static T ObsoleteReadGeneralSettings<T>(string connectionString)
        {
            var settingsStorage = new AzureBlobStorage(connectionString);
            var settingsData = settingsStorage.GetAsync("settings", "generalsettings.json").Result.ToBytes();
            var str = Encoding.UTF8.GetString(settingsData);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
        }

        public static T ReadGeneralSettings<T>(string url)
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(url) };
            var settingsData = httpClient.GetStringAsync("").Result;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(settingsData);
        }

        public static T ReadGeneralSettingsLocal<T>(string path)
        {
            var content = File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }
    }
}
