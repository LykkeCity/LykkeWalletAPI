using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.RabbitMq;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Messaging;
using Lykke.SettingsReader;
using LykkeApi2.Domain.Commands;
using LykkeApi2.Settings;

namespace LykkeApi2.Modules
{
    internal class CqrsModule : Module
    {
        private readonly IReloadingManager<BaseSettings> _settings;
        private readonly ILog _log;

        public CqrsModule(IReloadingManager<BaseSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Inceptum.Messaging.Serialization.MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.RabbitMq.ConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());
            
            builder.Register(ctx =>
            {
                return new CqrsEngine(_log,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        "RabbitMq",
                        "messagepack",
                        environment: "lykke",
                        exclusiveQueuePostfix: "k8s")),
                    
                    Register.BoundedContext("api")
                        .PublishingCommands(typeof(SignCommand)).To("wamp").With("commands"),
                    
                    Register.DefaultRouting
                        .PublishingCommands(typeof(SignCommand))
                            .To("wamp").With("commands")
                );
            })
            .As<ICqrsEngine>()
            .SingleInstance()
            .AutoActivate();
        }
    }
    
    internal class AutofacDependencyResolver : IDependencyResolver
    {
        private readonly IComponentContext _context;

        public AutofacDependencyResolver(IComponentContext kernel)
        {
            _context = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public object GetService(Type type)
        {
            return _context.Resolve(type);
        }

        public bool HasService(Type type)
        {
            return _context.IsRegistered(type);
        }
    }
}