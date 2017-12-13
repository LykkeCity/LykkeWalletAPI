using Autofac;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Modules
{
    public class AspNetCoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().InstancePerLifetimeScope();            
        }
    }
}