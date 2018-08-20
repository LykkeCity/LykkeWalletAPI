using System;
using System.Collections.Generic;
using System.Linq;
using Core.Exceptions;
using FluentValidation.Validators;
using Lykke.Common.Validation.NoSpecialCharacters;
using LykkeApi2.Middleware.LykkeApiError;
using LykkeApi2.Strings;

namespace LykkeApi2.Validation.Common
{
    public class NoSpecialCharactersFluentValidator
    {
        private readonly IDictionary<NoSpecialCharactersErrorCode, string> NoSpecialCharactersErrorMapping =
            new Dictionary<NoSpecialCharactersErrorCode, string>
            {
                {NoSpecialCharactersErrorCode.NullOrEmpty, Phrases.NotEmptyField},
                {
                    NoSpecialCharactersErrorCode.ContainsSpecialCharacters,
                    Phrases.NoSpecialCharactersError_ContainsSpecialCharacters
                }
            };

        private readonly NoSpecialCharactersValidator NoSpecialCharactersValidator;

        private static readonly string DefaultErrorMessage =
            Phrases.NoSpecialCharactersError_ContainsSpecialCharacters;

        public NoSpecialCharactersFluentValidator()
        {
            NoSpecialCharactersValidator = new NoSpecialCharactersValidator();
        }

        public NoSpecialCharactersFluentValidator(Action<INoSpecialCharactersConfigurationExpression> configAction)
        {
            NoSpecialCharactersValidator = new NoSpecialCharactersValidator(configAction);
        }

        public void Validate(string input, CustomContext context)
        {
            var result = NoSpecialCharactersValidator.Validate(input);

            if (result.IsValid) return;

            var errorCode = result.ErrorCodes.FirstOrDefault();

            var message = NoSpecialCharactersErrorMapping.ContainsKey(errorCode)
                ? NoSpecialCharactersErrorMapping[errorCode]
                : DefaultErrorMessage;

            context.AddFailure(string.Format(message, context.DisplayName));
        }
    }
}