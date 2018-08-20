using FluentValidation;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Common;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoverySubmitChallengeRequestModelValidator : AbstractValidator<RecoverySubmitChallengeRequestModel>
    {
        private readonly NoSpecialCharactersFluentValidator _valueValidator = new NoSpecialCharactersFluentValidator(
            c =>
            {
                c.AllowNull();
                c.AllowEmpty();
                c.SetAllowed('.', '-');
            });

        public RecoverySubmitChallengeRequestModelValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(x => x.StateToken)
                .Custom(StateTokenFluentValidator.Validate);
            RuleFor(x => x.Value)
                .Custom(_valueValidator.Validate);
        }
    }
}