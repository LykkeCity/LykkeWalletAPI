using System;
using FluentValidation;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Strings;
using LykkeApi2.Validation.Common;
using Action = Lykke.Service.ClientAccountRecovery.Client.Models.Enums.Action;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoverySubmitChallengeRequestModelValidator : AbstractValidator<RecoverySubmitChallengeRequestModel>
    {
        private readonly NoSpecialCharactersFluentValidator _valueValidator = new NoSpecialCharactersFluentValidator(
            c =>
            {
                c.AllowNull();
                c.AllowEmpty();
                c.SetAllowed('.', '-', '+', '/', '=');
            });

        public RecoverySubmitChallengeRequestModelValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(x => x.StateToken)
                .Custom(StateTokenFluentValidator.Validate);

            RuleFor(x => x.Value)
                .Custom(_valueValidator.Validate);

            RuleFor(x => x.Action)
                .NotNull()
                .NotEmpty()
                .Custom((s, context) =>
                {
                    if (Enum.TryParse<Action>(s, out _)) return;

                    var allowedValues = string.Join(',', Enum.GetNames(typeof(Action)));
                    var errorMessage = string.Format(Phrases.InvalidEnum, nameof(RecoverySubmitChallengeRequestModel.Action), allowedValues);
                    context.AddFailure(errorMessage);
                });
        }
    }
}