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
            var attr = context.ApiDescription.CustomAttributes().FirstOrDefault(x => x is ObsoleteAttribute);
            if (attr != null)
            {
                operation.Description += (attr as ObsoleteAttribute)?.Message;
            }
        }
    }
}
