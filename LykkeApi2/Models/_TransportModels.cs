using System;

namespace LykkeApi2.Models
{
    public class MyWebException : Exception
    {

        public MyWebException(ResponseModel.ErrorCodeType codeType, string message) : base(message)
        {
            CodeType = codeType;
        }

        public ResponseModel.ErrorCodeType CodeType { get; private set; }
    }


    public class ResponseModel
    {
        public ErrorModel Error { get; set; }

        public enum ErrorCodeType
        {
            InvalidInputField = 0,
            /// <summary>
            /// Returns, when request is being invoked but it should not be invoked acording to the current status
            /// </summary>
            InconsistentData = 1,
            NotAuthenticated = 2,
            InvalidUsernameOrPassword = 3,
            AssetNotFound = 4,
            NotEnoughFunds = 5,
            VersionNotSupported = 6,
            RuntimeProblem = 7,
            WrongConfirmationCode = 8,
            BackupWarning = 9,
            BackupRequired = 10,
            MaintananceMode = 11,

            NoData = 12,
            ShouldOpenNewChannel = 13,
            ShouldProvideNewTempPubKey = 14,
            ShouldProcesOffchainRequest = 15,
            NoOffchainLiquidity = 16,

            AddressShouldBeGenerated = 20,

            ExpiredAccessToken = 30,
            BadAccessToken = 31,
            NoEncodedMainKey = 32,
            PreviousTransactionsWereNotCompleted = 33,
            LimitationCheckFailed = 34,
            AssetAttributeNotFound = 35,

            BadRequest = 999,
            RquiredFileds = 40
        }

        public class ErrorModel
        {
            public ErrorCodeType Code { get; set; }
            /// <summary>
            /// In case ErrorCoderType = 0
            /// </summary>
            public string Field { get; set; }
            /// <summary>
            /// Localized Error message
            /// </summary>
            public string Message { get; set; }
        }

        public static ResponseModel CreateInvalidFieldError(string field, string message)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = message
                }
            };
        }

        public static ResponseModel CreateFail(ErrorCodeType errorCodeType, string message)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message
                }
            };
        }

        private static readonly ResponseModel OkInstance = new ResponseModel();

        public static ResponseModel CreateOk()
        {
            return OkInstance;
        }

    }

    public class ResponseModel<T> : ResponseModel
    {
        public T Result { get; set; }

        public static ResponseModel<T> CreateOk(T result)
        {
            return new ResponseModel<T>
            {
                Result = result
            };
        }

        public new static ResponseModel<T> CreateInvalidFieldError(string field, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = message
                }
            };
        }

        public new static ResponseModel<T> CreateModelStateInvalid(string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.RquiredFileds,
                    Field = "test",
                    Message = message
                }
            };
        }

        public new static ResponseModel<T> CreateFail(ErrorCodeType errorCodeType, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message
                }
            };
        }

        public static ResponseModel<T> CreateNotFound(ErrorCodeType errorCodeType, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message
                }
            };
        }
    }
}