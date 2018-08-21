using System.Collections.Generic;
using Autofac;
using Common.Log;
using Core.Settings;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Job.HistoryExportBuilder.Contract.Events;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Operations.Contracts.Commands;
using LykkeApi2.Cqrs.Projections;

namespace LykkeApi2.Modules
{
    public class CqrsModule : Module
    {
        private readonly APIv2Settings _settings;
        private readonly ILog _log;

        public CqrsModule(APIv2Settings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.SagasRabbitMq.RabbitConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();
            
            builder.RegisterType<HistoryExportProjection>().SingleInstance();

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());

            builder
                .Register(ctx =>
                {
                    const string defaultPipeline = "commands";
                    const string defaultRoute = "self";
                    
                    return new CqrsEngine(_log,
                        ctx.Resolve<IDependencyResolver>(),
                        messagingEngine,
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.MessagePack,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),
                        
                        Register.BoundedContext("apiv2")
                            .PublishingCommands(typeof(CreateCashoutCommand))
                                .To("operations").With("commands")
                            .ListeningEvents(
                                typeof(ClientHistoryExpiredEvent),
                                typeof(ClientHistoryExportedEvent))
                            .From(HistoryExportBuilderBoundedContext.Name).On(defaultRoute)
                            .WithProjection(typeof(HistoryExportProjection), HistoryExportBuilderBoundedContext.Name),
                        
                        
                        Register.DefaultRouting
                            .PublishingCommands(typeof(ExportClientHistoryCommand))
                            .To(HistoryExportBuilderBoundedContext.Name).With(defaultPipeline)
                    );
                })
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }
    }
}