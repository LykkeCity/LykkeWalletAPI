using Newtonsoft.Json;

namespace LykkeApi2.Models
{
    public class LykkeApiErrorResponse
    {
        [JsonProperty(PropertyName = "error")] 
        public string Error { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}