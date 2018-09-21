namespace LykkeApi2.Models.Operations
{
    public class CreateSwiftCashoutRequest
    {
        public string AssetId { get; set; }
        public decimal Volume { get; set; }

        public string Bic { get; set; }
        public string AccNumber { get; set; }
        public string AccName { get; set; }
        public string AccHolderAddress { get; set; }
        public string BankName { get; set; }

        public string AccHolderZipCode { get; set; }
        public string AccHolderCity { get; set; }
    }
}