using System;
using System.Net;

namespace Core.Exceptions
{
    public class ClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ClientMessage { get; }

        public ClientException(string message) : this(HttpStatusCode.BadRequest, message)
        {
            
        }

        public ClientException(HttpStatusCode status) : this(status, GetDefaultTextForStatus(status))
        {
            
        }

        public ClientException(HttpStatusCode code, string message)
        {
            StatusCode = code;
            ClientMessage = message;
        }

        private static string GetDefaultTextForStatus(HttpStatusCode status)
        {
            switch (status)
            {
                    case HttpStatusCode.NotFound: return "Resource not found";
                    case HttpStatusCode.BadRequest: return "Client generated bad request";
                    default: return "Something went wrong";
            }
        }
    }
}