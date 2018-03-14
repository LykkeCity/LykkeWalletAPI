using Autofac;
using AzureRepositories.Exchange;
using AzureRepositories.GlobalSettings;

namespace LykkeApi2.Modules
{
    public class DefaultValueModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register((c, p) => new AppGlobalSettings
            {
                DepositUrl = "http://mock-bankcards.azurewebsites.net/",
                DebugMode = true
            });
            builder.Register((c, p) => new ExchangeSettings
            {
                BaseAssetIos = string.Empty,
                BaseAssetOther = string.Empty,
                SignOrder = true
            });
        }
    }
}
