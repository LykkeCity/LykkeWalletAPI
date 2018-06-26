using Newtonsoft.Json;

namespace LykkeApi2.Models
{
    /// <summary>
    ///     General error response model.
    /// </summary>
    public class LykkeApiErrorResponse
    {
        /// <summary>
        ///     Unique error code, uniquely identifying occured error.
        /// </summary>
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        /// <summary>
        ///     Human-readable error message.
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}