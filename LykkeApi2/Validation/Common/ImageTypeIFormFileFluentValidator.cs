using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Validators;
using Lykke.Common.Validation.ImageTypes;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Validation.Common
{
    public class ImageTypeIFormFileFluentValidator : PropertyValidator
    {
        private static readonly IDictionary<ImageTypeErrorCode, string> ImageTypeErrorMapping =
            new Dictionary<ImageTypeErrorCode, string>
            {
                {ImageTypeErrorCode.FileNameNullOrWhitespace, Phrases.ImageTypeError_FileNameNullOrWhitespace},
                {
                    ImageTypeErrorCode.FileExtensionEmptyOrInvalid,
                    Phrases.ImageTypeError_FileExtensionEmptyOrInvalid
                },
                {ImageTypeErrorCode.FileStreamIsNull, Phrases.ImageTypeError_FileStreamIsNull},
                {ImageTypeErrorCode.FileStreamIsTooShort, Phrases.ImageTypeError_FileStreamIsTooShort},
                {ImageTypeErrorCode.InvalidHexSignature, Phrases.ImageTypeError_InvalidHexSignature}
            };

        private readonly ImageTypeValidator _imageTypeValidator;

        private static readonly string DefaultErrorMessage = Phrases.ImageTypeError_InvalidHexSignature;

        public ImageTypeIFormFileFluentValidator(params string[] extensions) : base(DefaultErrorMessage)
        {
            _imageTypeValidator = new ImageTypeValidator(extensions);
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (!(context.PropertyValue is IFormFile file)) return false;
            try
            {
                using (var fileStream = file.OpenReadStream())
                {
                    var result = _imageTypeValidator.Validate(file.FileName, fileStream);

                    if (result.IsValid)
                        return true;

                    var errorCode = result.ErrorCodes.FirstOrDefault();

                    var message = ImageTypeErrorMapping.ContainsKey(errorCode)
                        ? ImageTypeErrorMapping[errorCode]
                        : DefaultErrorMessage;

                    context.Rule.MessageBuilder = c =>
                        string.Format(message, context.DisplayName, result.AllowedExtensions);
                }
            }
            catch (ArgumentNullException)
            {
                const ImageTypeErrorCode errorCode = ImageTypeErrorCode.FileStreamIsNull;

                var message = ImageTypeErrorMapping.ContainsKey(errorCode)
                    ? ImageTypeErrorMapping[errorCode]
                    : DefaultErrorMessage;

                context.Rule.MessageBuilder = c =>
                    string.Format(message, context.DisplayName);
            }

            return false;
        }
    }
}