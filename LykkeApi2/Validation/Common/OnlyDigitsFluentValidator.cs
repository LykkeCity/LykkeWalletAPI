using System.Collections.Generic;
using System.Linq;
using FluentValidation.Validators;
using Lykke.Common.Validation.IsOnlyDigits;
using LykkeApi2.Strings;

namespace LykkeApi2.Validation.Common
{
    public static class OnlyDigitsFluentValidator
    {
        private static readonly IsOnlyDigitsValidator IsOnlyDigitsValidator = new IsOnlyDigitsValidator();

        private static readonly IDictionary<IsOnlyDigitsErrorCode, string> IsOnlyDigitsErrorMapping =
            new Dictionary<IsOnlyDigitsErrorCode, string>
            {
                {IsOnlyDigitsErrorCode.NullOrEmpty, Phrases.NotEmptyField},
                {IsOnlyDigitsErrorCode.NotOnlyDigits, Phrases.OnlyDigitsError_NotOnlyDigits}
            };

        private static readonly string DefaultErrorMessage = Phrases.OnlyDigitsError_NotOnlyDigits;

        public static void Validate(string input, CustomContext context)
        {
            var result = IsOnlyDigitsValidator.Validate(input);

            if (result.IsValid) return;

            var errorCode = result.ErrorCodes.FirstOrDefault();

            var message = IsOnlyDigitsErrorMapping.ContainsKey(errorCode)
                ? IsOnlyDigitsErrorMapping[errorCode]
                : DefaultErrorMessage;

            context.AddFailure(string.Format(message, context.DisplayName));
        }
    }
}