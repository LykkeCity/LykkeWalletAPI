using Autofac;
using Common.Log;
using Core.Settings;
using LykkeApi2.Controllers;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
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
            settings.WalletApiv2 = new BaseSettings { Db = new DbSettings(), Services = new ServiceSettings(), DeploymentSettings = new DeploymentSettings() };
            settings.WalletApiv2.Services.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.Services, mockUrl));
            //settings.WalletApiv2.DeploymentSettings.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.DeploymentSettings, mockUrl));

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Api2Module(settings, _mockLog.Object));
            containerBuilder.RegisterType<AssetsController>();
            containerBuilder.RegisterType<AccountExistController>();
            containerBuilder.RegisterType<RegistrationController>();

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
