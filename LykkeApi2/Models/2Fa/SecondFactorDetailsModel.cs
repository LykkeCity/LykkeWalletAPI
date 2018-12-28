using System.Runtime.Serialization;
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
        [EnumMember(Value = "google")]
        Google
    }

    public enum SecondFactorStatus
    {
        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "forbidden")]
        Forbidden
    }
}