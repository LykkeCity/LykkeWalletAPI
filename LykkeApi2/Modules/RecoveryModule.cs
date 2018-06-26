using Autofac;
using Common.Log;
using Core.Settings;
using LkeServices.Recovery;
using Lykke.Service.ClientAccountRecovery.Client;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class RecoveryModule : Module
    {
        private readonly ClientAccountRecoveryServiceClientSettings _сlientAccountRecoveryClientSettings;

        private readonly ClientAccountRecoverySettings _сlientAccountRecoverySettings;

        private readonly ILog _log;

        public RecoveryModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _log = log;
            _сlientAccountRecoveryClientSettings = settings.Nested(x => x.ClientRecoveryServiceClient).CurrentValue;
            _сlientAccountRecoverySettings = settings.Nested(x => x.ClientAccountRecoverySettings).CurrentValue;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new RecoveryFileSettings(_сlientAccountRecoverySettings?.SelfieImageMaxSizeMBytes, _log))
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterClientAccountRecoveryClient(_сlientAccountRecoveryClientSettings.ServiceUrl,
                _сlientAccountRecoveryClientSettings.ApiKey);

            builder.RegisterType<ClientAccountRecoveryService>().AsImplementedInterfaces().SingleInstance();
        }
    }
}