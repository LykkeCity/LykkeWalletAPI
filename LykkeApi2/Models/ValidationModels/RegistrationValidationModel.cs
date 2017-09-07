using Core.Messages;
using FluentValidation;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ValidationModels.RegistrationValidations;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels
{
    public class RegistrationValidationModel : AbstractValidator<AccountRegistrationModel>
    {
        private readonly IVerifiedEmailsRepository _verifiedEmailsRepository;
        private readonly IClientAccountClient _clientAccountClient;

        public RegistrationValidationModel(
            IVerifiedEmailsRepository verifiedEmailsRepository,
            IClientAccountClient clientAccountClient)
        {
            _verifiedEmailsRepository = verifiedEmailsRepository;
            _clientAccountClient = clientAccountClient;

            RuleFor(reg => reg.Email).NotNull();
            RuleFor(reg => reg.Hint).ValidHintVlue();
            RuleFor(reg => reg.Email).Must(IsEmaiVerified).WithMessage(Phrases.EmailNotVerified);
            //RuleFor(reg => reg.Email).Must(IsTraderWithEmailExistsForPartner).WithMessage(Phrases.ClientWithEmailIsRegistered);

        }

        private bool IsEmaiVerified(AccountRegistrationModel instance, string value)
        {
            return _verifiedEmailsRepository.IsEmailVerified(instance.Email, instance.PartnerId).Result;
        }

        //private bool IsTraderWithEmailExistsForPartner(AccountRegistrationModel instance, string foo)
        //{
        //    var result = _clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email;
        //    return result != null;
        //    //return !string.IsNullOrEmpty(_clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email);
        //}
    }
}
