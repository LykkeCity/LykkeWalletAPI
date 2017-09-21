using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories.Account;
using AzureRepositories.Email;
using AzureRepositories.Repositories;
using AzureStorage;
using AzureStorage.Tables;
using Common.IocContainer;
using Common.Log;
using Core.Accounts;
using Core.Messages;
using Core.Settings;
using FluentValidation.AspNetCore;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.ClientAccount.Client.Custom;
using Lykke.Service.Registration;
using Lykke.Service.Wallets.Client;
using LykkeApi2.App_Start;
using LykkeApi2.Credentials;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LykkeApi2.Modules
{
    public class Api2Module : Module
    {
        private readonly APIv2Settings _settings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;
        private TimeSpan DEFAULT_CACHE_EXPIRATION_PERIOD = TimeSpan.FromHours(1);

        public Api2Module(APIv2Settings settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            builder.RegisterInstance<IVerifiedEmailsRepository>(new VerifiedEmailsRepository(
              new AzureTableStorage<VerifiedEmailEntity>(_settings.WalletApiv2.Db.ClientPersonalInfoConnString, "VerifiedEmails", _log)));

            builder.RegisterInstance<DeploymentSettings>(new DeploymentSettings());
            builder.RegisterInstance(_settings.WalletApiv2.DeploymentSettings);

            _services.UseAssetsClient(AssetServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.AssetsServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD));
            _services.UseClientAccountService(ClientAccountServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.ClientAccountServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD));
            _services.UseClientAccountClient(ClientAccountServiceSettings.Create(new Uri(_settings.WalletApiv2.Services.ClientAccountServiceUrl), DEFAULT_CACHE_EXPIRATION_PERIOD), _log);

            //_services.AddSingleton<IVerifiedEmailsRepository>(new VerifiedEmailsRepository(
            //    new AzureTableStorage<VerifiedEmailEntity>(dbSettings.ClientPersonalInfoConnString, "VerifiedEmails", log)));

            _services.AddSingleton<ILykkeRegistrationClient>(x => new LykkeRegistrationClient(_settings.WalletApiv2.Services.RegistrationUrl, _log));

            _services.AddSingleton<IWalletsClient>(x => new WalletsClient(_settings.WalletApiv2.Services.WalletsServiceUrl, _log));

            _services.AddSingleton<ClientAccountLogic>();

            builder.Populate(_services);
        }
    }
}
