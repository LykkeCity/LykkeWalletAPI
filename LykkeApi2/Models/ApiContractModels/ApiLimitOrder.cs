namespace LykkeApi2.Models.ApiContractModels
{
    public class ApiLimitOrder
    {
        public string Id { get; set; }
        public string DateTime { get; set; }

        public string OrderType { get; set; }

        public double Volume { get; set; }
        public double RemainingVolume { get; set; }

        public double Price { get; set; }

        public string BaseAsset { get; set; }
        public string AssetPair { get; set; }

        public double TotalCost { get; set; }

        public int Accuracy { get; set; }
        public string OrderStatus { get; set; }
    }
}
