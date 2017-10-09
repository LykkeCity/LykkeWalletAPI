namespace LykkeApi2.Models.ApiContractModels
{
    public class ApiMarketOrder
    {
        public string Id { get; set; }
        public string DateTime { get; set; }

        public string OrderType { get; set; }

        public double Volume { get; set; }

        public double? Price { get; set; }

        public string BaseAsset { get; set; }
        public string AssetPair { get; set; }

        public double TotalCost { get; set; }
        public double Comission { get; set; }
        public double Position { get; set; }

        public int Accuracy { get; set; }
    }
}
