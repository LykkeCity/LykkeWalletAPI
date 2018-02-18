using Autofac;
using Common.Log;
using LykkeApi2.Controllers;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using Lykke.SettingsReader;
using LykkeApi2.Settings;
using Xunit;

namespace Lykke.WalletApiv2.Tests.DITests
{
    public class DITests
    {
        private readonly Mock<ILog> _mockLog;
        private readonly string mockUrl = "http://localhost";
        private readonly APIv2Settings settings;
        private IContainer container;

        public DITests()
        {
            _mockLog = new Mock<ILog>();

            settings = new APIv2Settings();
            settings.WalletApiv2 = new BaseSettings { Services = new ServiceSettings(), DeploymentSettings = new DeploymentSettings() };
            settings.WalletApiv2.Services.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.Services, mockUrl));
            //settings.WalletApiv2.DeploymentSettings.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.DeploymentSettings, mockUrl));

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new Api2Module(new SettingsServiceReloadingManager<APIv2Settings>("http://settings.lykke-settings.svc.cluster.local/rr5999apiv2999dvgsert25uwheifn_WalletApiv2")
                , _mockLog.Object));

            containerBuilder.RegisterModule(new ClientsModule(new SettingsServiceReloadingManager<APIv2Settings>("http://settings.lykke-settings.svc.cluster.local/rr5999apiv2999dvgsert25uwheifn_WalletApiv2"), _mockLog.Object));

            containerBuilder.RegisterType<AssetsController>();

            //register your controller class here to test

            this.container = containerBuilder.Build();
        }


        [Fact]
        public void Test_InstantiateControllers()
        {
            //Arrange
            var controllersToTest = container.ComponentRegistry.Registrations.Where(r => typeof(Controller).IsAssignableFrom(r.Activator.LimitType)).Select(r => r.Activator.LimitType).ToList();
            controllersToTest.ForEach(controller =>
            {
                //Act-Assert - ok if no exception
                this.container.Resolve(controller);
            });
        }
    }
}
