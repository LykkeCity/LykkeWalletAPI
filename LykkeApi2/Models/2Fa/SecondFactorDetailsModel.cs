using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models._2Fa
{
    public class SecondFactorDetailsModel
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SecondFactorType Type { set; get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SecondFactorStatus Status { set; get; }
    }

    public enum SecondFactorType
    {
        Google
    }

    public enum SecondFactorStatus
    {
        Active,
        Forbidden
    }
}