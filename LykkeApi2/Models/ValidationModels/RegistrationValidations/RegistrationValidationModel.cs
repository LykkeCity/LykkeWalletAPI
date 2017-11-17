using Core.Constants;
using Core.Settings;
using FluentValidation;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels.RegistrationValidations
{
    public class RegistrationValidationModel : AbstractValidator<AccountRegistrationModel>
    {
        private readonly IClientAccountClient _clientAccountService;
        private readonly DeploymentSettings _deploymentSettings;

        public RegistrationValidationModel(
            IClientAccountClient clientAccountService,
            DeploymentSettings deploymentSettings)
        {
            _clientAccountService = clientAccountService;
            _deploymentSettings = deploymentSettings;

            RegisterRules();
        }

        private bool IsEmaiVerified(AccountRegistrationModel instance, string value)
        {
            return !_deploymentSettings.IsProduction ||
                   (_clientAccountService.IsEmailVerifiedAsync(instance.Email, instance.PartnerId).Result ?? false);
        }

        private void RegisterRules()
        {
            #region Email
            RuleFor(reg => reg.Email).NotNull().WithMessage(Phrases.FieldShouldNotBeEmpty);
            RuleFor(reg => reg.Email).EmailAddress().WithMessage(Phrases.InvalidEmailFormat);
            RuleFor(reg => reg.Email).Must(IsEmaiVerified).WithMessage(Phrases.EmailNotVerified);
            #endregion

            #region Password
            RuleFor(reg => reg.Password).NotNull().WithMessage(Phrases.FieldShouldNotBeEmpty);
            RuleFor(reg => reg.Password).MinimumLength(LykkeConstants.MinPwdLength)
                .WithMessage(string.Format(Phrases.MinLength, LykkeConstants.MinPwdLength));
            RuleFor(reg => reg.Password).MaximumLength(LykkeConstants.MaxPwdLength)
                .WithMessage(string.Format(Phrases.MaxLength, LykkeConstants.MaxPwdLength));
            #endregion

            #region Hint
            RuleFor(reg => reg.Hint).ValidHintValue().WithMessage(Phrases.InvalidValue);
            #endregion
        }
    }
}