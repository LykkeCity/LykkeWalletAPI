using System;
using System.Net;

namespace Core.Exceptions
{
    public class ClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ExceptionType ExceptionType { get; }
        
        public ClientException(HttpStatusCode code, ExceptionType message)
        {
            StatusCode = code;
            ExceptionType = message;
        }
        
        public ClientException(ExceptionType message) : this(HttpStatusCode.BadRequest, message)
        {
        }

        public static string GetTextForException(ExceptionType exceptionType)
        {
            switch (exceptionType)
            {
                case ExceptionType.AssetNotFound:
                    return "Asset not found";
                case ExceptionType.AssetUnavailable:
                    return "Asset unavailable";
                case ExceptionType.PendingDialogs:
                    return "Pending dialogs";
                case ExceptionType.AddressNotGenerated:
                    return "Address not generated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(exceptionType), exceptionType, null);
            }
        }
    }

    public enum ExceptionType
    {
        AssetNotFound,
        AssetUnavailable,
        PendingDialogs,
        AddressNotGenerated
    }
}