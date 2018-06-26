using Core.Constants;
using FluentValidation;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Common;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoveryStartRequestModelValidator : AbstractValidator<RecoveryStartRequestModel>
    {
        private readonly NoSpecialCharactersFluentValidator _partnerIdValidator = new NoSpecialCharactersFluentValidator(
            c =>
            {
                c.AllowNull();
                c.AllowEmpty();
                c.SetAllowed('-');
            });

        public RecoveryStartRequestModelValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(x => x.Email)
                .NotNull()
                .NotEmpty()
                .MaximumLength(LykkeConstants.MaxEmailLength)
                .EmailAddress();

            RuleFor(x => x.PartnerId)
                .Custom(_partnerIdValidator.Validate)
                .MaximumLength(LykkeConstants.MaxFieldLength);
        }
    }
}