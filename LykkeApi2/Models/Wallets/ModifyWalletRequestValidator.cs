using FluentValidation;

namespace LykkeApi2.Models.Wallets
{
    public class ModifyWalletRequestValidator : AbstractValidator<ModifyWalletRequest>
    {
        public ModifyWalletRequestValidator()
        {
            RuleFor(m => m.Name).NotEmpty();
        }
    }
}