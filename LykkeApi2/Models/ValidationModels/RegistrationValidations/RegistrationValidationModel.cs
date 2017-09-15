using Core.Constants;
using Core.Messages;
using Core.Settings;
using FluentValidation;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels.RegistrationValidations
{
    public class RegistrationValidationModel : AbstractValidator<AccountRegistrationModel>
    {
        private readonly IVerifiedEmailsRepository _verifiedEmailsRepository;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly DeploymentSettings _deploymentSettings;
        private readonly ClientAccountLogic _clientAccountLogic;

        public RegistrationValidationModel(
            IVerifiedEmailsRepository verifiedEmailsRepository,
            IClientAccountClient clientAccountClient,
            DeploymentSettings deploymentSettings,
            ClientAccountLogic clientAccountLogic)
        {
            _verifiedEmailsRepository = verifiedEmailsRepository;
            _clientAccountClient = clientAccountClient;
            _deploymentSettings = deploymentSettings;
            _clientAccountLogic = clientAccountLogic;

            RuleFor(reg => reg.Email).NotNull().WithMessage(Phrases.FieldShouldNotBeEmpty);
            RuleFor(reg => reg.Email).EmailAddress().WithMessage(Phrases.InvalidEmailFormat);
            RuleFor(reg => reg.Email).Must(IsEmaiVerified).WithMessage(Phrases.EmailNotVerified);

            RuleFor(reg => reg.Password).NotNull().WithMessage(Phrases.FieldShouldNotBeEmpty);
            RuleFor(reg => reg.Password).MinimumLength(LykkeConstants.MinPwdLength).WithMessage(string.Format(Phrases.MinLength, LykkeConstants.MinPwdLength));
            RuleFor(reg => reg.Password).MaximumLength(LykkeConstants.MaxPwdLength).WithMessage(string.Format(Phrases.MaxLength, LykkeConstants.MaxPwdLength));

            RuleFor(reg => reg.Hint).ValidHintVlue().WithMessage(Phrases.InvalidValue);
        }

        private bool IsEmaiVerified(AccountRegistrationModel instance, string value)
        {
            return (!_deploymentSettings.IsProduction) || (_clientAccountClient.IsEmailVerified(new VerifiedEmailModel()
            {
                Email = instance.Email,
                PartnerId = instance.PartnerId
            }).Result ?? false);
        }
    }
}
