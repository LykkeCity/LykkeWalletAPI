using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LykkeApi2.Infrastructure.Authentication
{
    public static class LykkeOAuthAuthenticationExtension
    {
        public static AuthenticationBuilder CustomizeServerAuthentication(this AuthenticationBuilder builder)
        {
            builder.Services.Replace(new ServiceDescriptor(typeof(OAuth2IntrospectionHandler), typeof(LykkeAuthHandler), ServiceLifetime.Transient));
            return builder;
        }
    }
}