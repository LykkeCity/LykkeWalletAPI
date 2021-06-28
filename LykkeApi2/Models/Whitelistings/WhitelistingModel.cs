using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.Whitelistings
{
    public class WhitelistingModel : WhitelistingBaseModel
    {
        public string Id { set; get; }
        public DateTime CreatedAt { set; get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public WhitelistingStatus Status { set; get; }
    }
}
