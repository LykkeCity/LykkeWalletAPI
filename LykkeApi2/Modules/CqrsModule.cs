using System.Collections.Generic;
using Autofac;
using Common.Log;
using Core.Settings;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class CqrsModule : Module
    {
        private readonly IReloadingManager<APIv2Settings> _settings;
        private readonly ILog _log;

        public CqrsModule(IReloadingManager<APIv2Settings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var rabbitMqSagasSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.CurrentValue.SagasRabbitMq.RabbitConnectionString
            };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            builder.Register(ctx => new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "SagasRabbitMq",
                        new TransportInfo(rabbitMqSagasSettings.Endpoint.ToString(), rabbitMqSagasSettings.UserName,
                            rabbitMqSagasSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory())).As<IMessagingEngine>();

            var sagasEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                SerializationFormat.MessagePack,
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.Register(ctx =>
                {
                    return new CqrsEngine(
                        _log,
                        ctx.Resolve<IDependencyResolver>(),
                        ctx.Resolve<IMessagingEngine>(),
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(sagasEndpointResolver),

                        Register.BoundedContext("operations-api")
                            .PublishingCommands(typeof(CreateCashoutCommand))
                                .To("operations").With("commands")
                    );
                })
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }
    }
}