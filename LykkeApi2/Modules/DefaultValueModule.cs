using Autofac;
using AzureRepositories.Exchange;
using AzureRepositories.GlobalSettings;
using Core.Exchange;
using Core.GlobalSettings;

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
            }).As<IAppGlobalSettings>();

            builder.Register((c, p) => new ExchangeSettings
            {
                BaseAssetIos = string.Empty,
                BaseAssetOther = string.Empty,
                SignOrder = true
            }).As<IExchangeSettings>();
        }
    }
}
