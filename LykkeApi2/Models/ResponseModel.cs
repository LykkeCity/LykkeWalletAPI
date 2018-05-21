namespace LykkeApi2.Models
{
    public class ResponseModel
    {
        private static readonly ResponseModel OkInstance = new ResponseModel();
        public ErrorModel Error { get; set; }

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

        public static ResponseModel CreateInvalidFieldErrorWithTemplate(string field, string message)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = string.Format(message, field)
                }
            };
        }

        public static ResponseModel CreateFail(ErrorCodeType errorCodeType, string message, object details = null, string field = null)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message,
                    Field = field,
                    Details = details
                }
            };
        }

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

        public new static ResponseModel<T> CreateInvalidFieldErrorWithTemplate(string field, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = string.Format(message, field)
                }
            };
        }

        public static ResponseModel<T> CreateFail(ErrorCodeType errorCodeType, string message)
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