using System;
using System.Net;

namespace Core.Exceptions
{
    public class ClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ExceptionType ExceptionType { get; }
        
        public ClientException(HttpStatusCode code, ExceptionType exceptionType)
        {
            StatusCode = code;
            ExceptionType = exceptionType;
        }
        
        public ClientException(ExceptionType exceptionType) : this(HttpStatusCode.BadRequest, exceptionType)
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
                case ExceptionType.KycRequired:
                    return "KYC required for this operation";
                case ExceptionType.LimitReached:
                    return "The limit is reached";
                case ExceptionType.InvalidInput:
                    return "One of the provided values was not valid";
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
        AddressNotGenerated,
        KycRequired,
        LimitReached,
        InvalidInput
    }
}