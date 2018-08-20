using Core.Constants;
using FluentValidation;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Common;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoveryCompleteRequestModelValidator : AbstractValidator<RecoveryCompleteRequestModel>
    {
        private const int PinMaxLength = 4;

        private const int HintMaxLength = 128;

        private readonly NoSpecialCharactersFluentValidator _hintValidator = new NoSpecialCharactersFluentValidator(
            c => { c.SetAllowed(',', '.'); });

        public RecoveryCompleteRequestModelValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(x => x.StateToken)
                .Custom(StateTokenFluentValidator.Validate);
            RuleFor(x => x.PasswordHash)
                .Custom(PasswordHashFluentValidator.Validate);
            RuleFor(x => x.Pin)
                .NotNull()
                .NotEmpty()
                .Length(PinMaxLength)
                .Custom(OnlyDigitsFluentValidator.Validate);
            RuleFor(x => x.Hint)
                .MaximumLength(HintMaxLength)
                .Custom(_hintValidator.Validate);
        }
    }
}