using Autofac;
using Common.Log;
using LykkeApi2.Controllers;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using Core.Settings;
using Lykke.Service.Affiliate.Client;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDictionaries.Client;
using Lykke.Service.Kyc.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PersonalData.Settings;
using Lykke.Service.SwiftCredentials.Client;
using Lykke.SettingsReader.ReloadingManager;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.DITests
{
    [TestFixture]
    public class DiTests
    {
        private const string MockUrl = "http://localhost";
        private IContainer _container;

        [SetUp]
        public void Init()
        {
            var mockLog = new Mock<ILog>();

            var settings = new APIv2Settings
            {
                GlobalSettings = new GlobalSettings(),
                FeeSettings = new FeeSettings(),
                IcoSettings = new IcoSettings(),
                KycServiceClient = new KycServiceClientSettings(),
                WalletApiv2 = new BaseSettings
                {
                    Db = new DbSettings(),
                    Services = new ServiceSettings
                    {
                        AffiliateServiceClient = new AffiliateServiceClientSettings { ServiceUrl = MockUrl }
                    },
                    DeploymentSettings = new DeploymentSettings(),
                    CacheSettings = new CacheSettings(),                    
                },                
                ClientDictionariesServiceClient = new ClientDictionariesServiceClientSettings() { ServiceUrl = MockUrl },
                FeeCalculatorServiceClient = new FeeCalculatorSettings{ServiceUrl = MockUrl},
                PersonalDataServiceSettings = new PersonalDataServiceClientSettings{ServiceUri = MockUrl},
                MatchingEngineClient = new MatchingEngineSettings{IpEndpoint = new IpEndpointSettings{Host = "127.0.0.1", Port = 80}},
				AssetDisclaimersServiceClient = new AssetDisclaimersServiceClientSettings { ServiceUrl = MockUrl },
                PaymentSystemServiceClient = new PaymentSystemServiceClientSettings { ServiceUrl = MockUrl },
                LimitationServiceClient = new LimitationServiceSettings { ServiceUrl = MockUrl },
                ClientDialogsServiceClient = new ClientDialogsServiceClientSettings { ServiceUrl = MockUrl },
                SwiftCredentialsServiceClient = new SwiftCredentialsServiceClientSettings { ServiceUrl = MockUrl },
                BlockchainCashoutPreconditionsCheckServiceClient =
                    new BlockchainCashoutPreconditionsCheckServiceClientSettings { ServiceUrl = MockUrl }
                
            };
            settings.WalletApiv2.Services.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.Services, MockUrl));

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new Api2Module(ConstantReloadingManager.From(settings), mockLog.Object));
            containerBuilder.RegisterModule(new ClientsModule(ConstantReloadingManager.From(settings), mockLog.Object));
            containerBuilder.RegisterModule(new AspNetCoreModule());

            containerBuilder.RegisterType<AssetsController>();

            //register your controller class here to test
            _container = containerBuilder.Build();
        }

        [Test]
        public void Test_InstantiateControllers()
        {
            //Arrange
            var controllersToTest = _container.ComponentRegistry.Registrations.Where(r => typeof(Controller).IsAssignableFrom(r.Activator.LimitType)).Select(r => r.Activator.LimitType).ToList();
            controllersToTest.ForEach(controller =>
            {
                //Act-Assert - ok if no exception
                _container.Resolve(controller);
            });
        }
    }
}
