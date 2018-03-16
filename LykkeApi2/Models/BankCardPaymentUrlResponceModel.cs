namespace LykkeApi2.Models
{
    public class BankCardPaymentUrlResponseModel
    {
        public string Url { get; set; }
        public string OkUrl { get; set; }
        public string FailUrl { get; set; }
        public string ReloadRegex { get; set; }
        /// <summary>
        /// Regular expression which will be used on client to determine should url be sent to
        /// api/FormatCreditVouchersContent to format it or shouldn't
        /// </summary>
        public string UrlsToFormatRegex { get; set; }
    }
}