using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.ResponceModels
{
    public class ApiResponse
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; }

        public ApiResponse(ResponseStatusCode statusCode, string message = null)
        {
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }

        private static string GetDefaultMessageForStatusCode(ResponseStatusCode statusCode)
        {
            switch (statusCode)
            {
                case ResponseStatusCode.NotFound:
                    return "Resource not found";
                case ResponseStatusCode.InternalServerError:
                    return "An unhandled error occurred";
                default:
                    return null;
            }
        }
    }

    public enum ResponseStatusCode
    {
        NotFound = 404,
        InternalServerError = 500,
        OK = 200,
        BadRequest = 400,
        NoData = 12
    }
}
