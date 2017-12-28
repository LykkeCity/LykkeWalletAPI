namespace Core.ExchangeSettings
{
    public static class ExchangeSettingsRepositoryExt
    {
        public static string BaseAsset(this IExchangeSettings settings, bool isIosDevice)
        {
            return isIosDevice ? settings.BaseAssetIos : settings.BaseAssetOther;
        }
    }
}