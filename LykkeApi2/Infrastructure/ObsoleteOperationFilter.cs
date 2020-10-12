using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Infrastructure
{
    public class ObsoleteOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.TryGetMethodInfo(out var methodinfo))
                return;

            var attr = methodinfo.GetCustomAttributes(false).OfType<ObsoleteAttribute>().FirstOrDefault();
            if (attr != null)
            {
                operation.Description += attr.Message;
            }
        }
    }
}
