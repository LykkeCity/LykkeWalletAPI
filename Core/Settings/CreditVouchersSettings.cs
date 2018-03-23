namespace Core.Settings
{
    public class CreditVouchersSettings
    {
        public double MinAmount { get; set; }
        public double MaxAmount { get; set; }
        public string[] ServiceUrls { get; set; }
        public string[] SupportedCurrencies { get; set; }
    }
}