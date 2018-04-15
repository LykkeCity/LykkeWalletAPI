namespace LykkeApi2.Models.AssetPairsModels
{
    public class AssetPairModel
    {
        public string Id { get; set; }
        public int Accuracy { get; set; }
        public string BaseAssetId { get; set; }
        public int InvertedAccuracy { get; set; }
        public string Name { get; set; }
        public string QuotingAssetId { get; set; }
    }
}