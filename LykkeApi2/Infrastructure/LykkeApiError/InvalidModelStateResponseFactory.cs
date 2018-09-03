using System.Linq;
using Core.Constants;
using Core.Exceptions;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LykkeApi2.Infrastructure.LykkeApiError
{
    public static class InvalidModelStateResponseFactory
    {
        /// <summary>
        ///     General validation error processing delegate.
        ///     Wraps any failed model validation into <see cref="LykkeApiErrorResponse" />.
        ///     To return custom error code, throw <see cref="LykkeApiErrorException" /> from validator
        ///     with appropriate code from <see cref="LykkeApiErrorCodes.ModelValidation" />.
        ///     If code does not exist feel free to create a new one.
        /// </summary>
        public static IActionResult CreateInvalidModelResponse(ActionContext context)
        {
            {
                var apiErrorResponse = new LykkeApiErrorResponse
                {
                    Error = LykkeApiErrorCodes.ModelValidation.ModelValidationFailed.Name,
                    Message = GetErrorMessage(context.ModelState)
                };
                return new BadRequestObjectResult(apiErrorResponse)
                {
                    ContentTypes = {"application/json"}
                };
            }
        }

        private static string GetErrorMessage(ModelStateDictionary modelStateDictionary)
        {
            var modelError = modelStateDictionary?.Values.FirstOrDefault()?.Errors.FirstOrDefault();

            if (modelError == null)
                return string.Empty;

            return modelError.Exception != null 
                ? modelError.Exception.Message 
                : modelError.ErrorMessage;
        }
    }
}