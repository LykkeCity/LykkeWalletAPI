using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Core.Settings;
using LkeServices.Exchange;
using LkeServices.GlobalSettings;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

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
