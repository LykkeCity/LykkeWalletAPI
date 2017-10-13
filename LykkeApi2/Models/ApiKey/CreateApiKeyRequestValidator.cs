using FluentValidation;

namespace LykkeApi2.Models.ApiKey
{
    public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
    {
        public CreateApiKeyRequestValidator()
        {
        }
    }
}