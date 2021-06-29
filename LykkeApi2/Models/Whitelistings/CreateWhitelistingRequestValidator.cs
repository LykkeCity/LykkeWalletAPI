using FluentValidation;

namespace LykkeApi2.Models.Whitelistings
{
    public class CreateWhitelistingRequestValidator : AbstractValidator<CreateWhitelistingRequest>
    {
        public CreateWhitelistingRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.AddressBase).NotEmpty();
            RuleFor(x => x.Code2Fa).NotEmpty();
        }
    }
}
