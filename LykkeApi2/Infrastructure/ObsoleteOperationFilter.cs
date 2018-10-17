using System;
using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Infrastructure
{
    public class ObsoleteOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var attr = context.ApiDescription.ActionAttributes().FirstOrDefault(x => x is ObsoleteAttribute);
            if (attr != null)
            {
                operation.Description += (attr as ObsoleteAttribute).Message;
            }
        }
    }
}