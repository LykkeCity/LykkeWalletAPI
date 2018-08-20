using System.Collections.Generic;
using System.Linq;
using FluentValidation.Validators;
using Lykke.Common.Validation.PasswordHash;
using LykkeApi2.Strings;

namespace LykkeApi2.Validation.Common
{
    public static class PasswordHashFluentValidator
    {
        private static readonly PasswordHashValidator PasswordHashValidator = new PasswordHashValidator();

        private static readonly IDictionary<PasswordHashErrorCode, string> PasswordHashErrorMapping =
            new Dictionary<PasswordHashErrorCode, string>
            {
                {PasswordHashErrorCode.NullOrEmpty, Phrases.NotEmptyField},
                {PasswordHashErrorCode.NotSha256, Phrases.PasswordHashError_NotSha256}
            };

        private static readonly string DefaultErrorMessage = Phrases.PasswordHashError_NotSha256;

        public static void Validate(string input, CustomContext context)
        {
            var result = PasswordHashValidator.Validate(input);

            if (result.IsValid) return;

            var errorCode = result.ErrorCodes.FirstOrDefault();

            var message = PasswordHashErrorMapping.ContainsKey(errorCode)
                ? PasswordHashErrorMapping[errorCode]
                : DefaultErrorMessage;

            context.AddFailure(string.Format(message, context.DisplayName));
        }
    }
}