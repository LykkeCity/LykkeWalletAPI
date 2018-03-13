namespace Core.PaymentSystem
{
    public class PaymentUrlData
    {
        public string PaymentUrl { get; set; }
        public string OkUrl { get; set; }
        public string FailUrl { get; set; }
        public string ReloadRegexp { get; set; }
        public string UrlsRegexp { get; set; }
        public string ErrorMessage { get; set; }
        public CashInPaymentSystem PaymentSystem { get; set; }
    }
}