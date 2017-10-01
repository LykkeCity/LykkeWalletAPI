using Autofac;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Modules
{
    public class AspNetCoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().SingleInstance();
            builder.RegisterType<RequestContext>().As<IRequestContext>().OwnedByLifetimeScope();
        }
    }
}