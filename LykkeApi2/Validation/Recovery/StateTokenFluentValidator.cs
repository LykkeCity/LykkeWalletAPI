using System.Collections.Generic;
using System.Linq;
using FluentValidation.Validators;
using Lykke.Common.Validation.JweToken;
using LykkeApi2.Strings;

namespace LykkeApi2.Validation.Recovery
{
    // Pay attention that for recovery process we are using JWE tokens only.
    public static class StateTokenFluentValidator
    {
        private static readonly JweTokenValidator JweTokenValidator = new JweTokenValidator();

        private static readonly IDictionary<JweTokenErrorCode, string> JweTokenErrorMapping =
            new Dictionary<JweTokenErrorCode, string>
            {
                {JweTokenErrorCode.NullOrEmpty, Phrases.NotEmptyField},
                {JweTokenErrorCode.NotJweToken, Phrases.JweTokenError_NotJweToken}
            };

        private static readonly string DefaultErrorMessage = Phrases.JweTokenError_NotJweToken;

        public static void Validate(string input, CustomContext context)
        {
            var result = JweTokenValidator.Validate(input);

            if (result.IsValid) return;

            var errorCode = result.ErrorCodes.FirstOrDefault();

            var message = JweTokenErrorMapping.ContainsKey(errorCode)
                ? JweTokenErrorMapping[errorCode]
                : DefaultErrorMessage;

            context.AddFailure(string.Format(message, context.DisplayName));
        }
    }
}
