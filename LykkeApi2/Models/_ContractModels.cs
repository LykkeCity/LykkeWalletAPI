namespace LykkeApi2.Models
{
    public class AssetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int Accuracy { get; set; }
        public bool KycNeeded { get; set; }
        public bool BankCardsDepositEnabled { get; set; }
        public bool SwiftDepositEnabled { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public string CategoryId { get; set; }
        public bool IsBase { get; set; }
        public bool CanBeBase { get; set; }
        public string IconUrl { get; set; }
    }

    public class AssetDescriptionModel
    {
        public string Id { get; set; }
        public string AssetClass { get; set; }
        public string Description { get; set; }
        public string IssuerName { get; set; }
        public string NumberOfCoins { get; set; }
        public string AssetDescriptionUrl { get; set; }
        public string FullName { get; set; }
    }

    public class AssetCategoryModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int? SortOrder { get; set; }
    }
}