namespace LykkeApi2.Models
{
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

        public object Details { get; set; }
    }
}