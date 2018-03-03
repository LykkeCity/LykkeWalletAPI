using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LykkeApi2.Infrastructure.Extensions
{
    public static class ModelStateDictionaryExtensions
    {
        public static string GetErrorMessage(this ModelStateDictionary modelState)
        {
            foreach (var state in modelState)
            {
                var message = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList().FirstOrDefault();

                if (string.IsNullOrEmpty(message))
                    continue;

                return message;
            }

            return string.Empty;
        }
    }
}
