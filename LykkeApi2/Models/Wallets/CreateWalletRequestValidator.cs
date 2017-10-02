using FluentValidation;

namespace LykkeApi2.Models.Wallets
{
    public class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequest>
    {
        public CreateWalletRequestValidator()
        {
            RuleFor(m => m.ClientId).NotEmpty();
            RuleFor(m => m.Type).NotEmpty();
        }
    }
}