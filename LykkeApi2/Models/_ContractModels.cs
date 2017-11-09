namespace LykkeApi2.Models
{
    public class ApiAssetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int Accuracy { get; set; }
        public string Symbol { get; set; }
        public bool HideWithdraw { get; set; }
        public bool HideDeposit { get; set; }
        public bool KycNeeded { get; set; }
        public bool BankCardsDepositEnabled { get; set; }
        public bool SwiftDepositEnabled { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public string CategoryId { get; set; }
    }

    public class AssetDescriptionModel
    {
        public string Id { get; set; }
        public string AssetClass { get; set; }
        public int PopIndex { get; set; }
        public string Description { get; set; }
        public string IssuerName { get; set; }
        public string NumberOfCoins { get; set; }
        public string MarketCapitalization { get; set; }
        public string AssetDescriptionUrl { get; set; }
        public string FullName { get; set; }
    }

    public class ApiAssetCategoryModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IosIconUrl { get; set; }
        public string AndroidIconUrl { get; set; }
        public int? SortOrder { get; set; }
    }
}
