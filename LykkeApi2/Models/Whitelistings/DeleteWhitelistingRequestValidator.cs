using FluentValidation;

namespace LykkeApi2.Models.Whitelistings
{
    public class DeleteWhitelistingRequestValidator : AbstractValidator<DeleteWhitelistingRequest>
    {
        public DeleteWhitelistingRequestValidator()
        {
            RuleFor(x => x.Code2Fa).NotEmpty();
        }
    }
}
