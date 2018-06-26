using Autofac;
using AutoMapper;
using LykkeApi2.Automapper;

namespace LykkeApi2.Modules
{
    public class AutomapperModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new MapperConfiguration(cfg => { cfg.AddProfile<RecoveryAutomapperProfile>(); }))
                .AsSelf()
                .SingleInstance();

            builder.Register(c => c.Resolve<MapperConfiguration>().CreateMapper(c.Resolve))
                .AsImplementedInterfaces()
                .SingleInstance();
        }
    }
}