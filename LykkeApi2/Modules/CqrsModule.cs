using System;
using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Job.HistoryExportBuilder.Contract.Events;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using LykkeApi2.Cqrs.Projections;

namespace LykkeApi2.Modules
{
    public class CqrsModule : Module
    {
        private readonly APIv2Settings _settings;

        public CqrsModule(APIv2Settings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(_settings.SagasRabbitMq.RabbitConnectionString)
            };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            builder.Register(ctx => new MessagingEngine(ctx.Resolve<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory(ctx.Resolve<ILogFactory>()))).As<IMessagingEngine>().SingleInstance();


            builder.RegisterType<HistoryExportProjection>().SingleInstance();

            builder
                .Register(ctx =>
                {
                    const string defaultPipeline = "commands";
                    const string defaultRoute = "self";

                    var engine = new CqrsEngine(ctx.Resolve<ILogFactory>(),
                        ctx.Resolve<IDependencyResolver>(),
                        ctx.Resolve<IMessagingEngine>(),
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.MessagePack,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                        Register.BoundedContext("apiv2")
                            .PublishingCommands(typeof(CreateCashoutCommand), typeof(CreateSwiftCashoutCommand))
                                .To(OperationsBoundedContext.Name).With(defaultPipeline)
                            .ListeningEvents(
                                typeof(ClientHistoryExpiredEvent),
                                typeof(ClientHistoryExportedEvent))
                            .From(HistoryExportBuilderBoundedContext.Name).On(defaultRoute)
                            .WithProjection(typeof(HistoryExportProjection), HistoryExportBuilderBoundedContext.Name)
                            .PublishingCommands(typeof(ConfirmCommand))
                                .To(OperationsBoundedContext.Name).With(defaultPipeline),

                        Register.DefaultRouting
                            .PublishingCommands(typeof(ExportClientHistoryCommand))
                            .To(HistoryExportBuilderBoundedContext.Name).With(defaultPipeline)
                    );
                    engine.StartPublishers();
                    return engine;
                })
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }
    }
}
