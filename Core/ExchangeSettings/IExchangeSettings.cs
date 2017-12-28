namespace Core.ExchangeSettings
{
    public interface IExchangeSettings
    {
        string BaseAssetIos { get; }
        string BaseAssetOther { get; }
        bool SignOrder { get; }
    }
}