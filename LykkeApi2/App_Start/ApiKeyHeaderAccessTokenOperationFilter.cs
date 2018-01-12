using System.Collections.Generic;
using System.Linq;
using LykkeApi2.Attributes;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2
{
    public class ApiKeyHeaderAccessTokenOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var isTokenAccess = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is SignatureVerificationAttribute);
            if (isTokenAccess)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();
                
                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "SignatureVerificationToken",
                    In = "header",
                    Description = "signature verification token",
                    Required = true,
                    Type = "string"
                });
            }
        }
    }
}