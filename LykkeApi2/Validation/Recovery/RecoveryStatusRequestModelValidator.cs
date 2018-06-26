using FluentValidation;
using LykkeApi2.Models.Recovery;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoveryStatusRequestModelValidator : AbstractValidator<RecoveryStatusRequestModel>
    {
        public RecoveryStatusRequestModelValidator()
        {
            RuleFor(x => x.StateToken)
                .Custom(StateTokenFluentValidator.Validate);
        }
    }
}