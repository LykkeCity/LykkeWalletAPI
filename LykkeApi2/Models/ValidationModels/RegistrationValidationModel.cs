using FluentValidation;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ValidationModels.RegistrationValidations;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels
{
    public class RegistrationValidationModel : AbstractValidator<AccountRegistrationModel>
    {        
        private readonly IClientAccountClient _clientAccountService;
        
        public RegistrationValidationModel(IClientAccountClient clientAccountService)
        {            
            _clientAccountService = clientAccountService;
            
            RuleFor(reg => reg.Email).NotNull();
            RuleFor(reg => reg.Hint).ValidHintValue();
            RuleFor(reg => reg.Email).Must(IsEmaiVerified).WithMessage(Phrases.EmailNotVerified);
            //RuleFor(reg => reg.Email).Must(IsTraderWithEmailExistsForPartner).WithMessage(Phrases.ClientWithEmailIsRegistered);

        }

        private bool IsEmaiVerified(AccountRegistrationModel instance, string value)
        {
            return _clientAccountService.IsEmailVerifiedAsync(instance.Email, instance.PartnerId).Result ?? false;
        }

        //private bool IsTraderWithEmailExistsForPartner(AccountRegistrationModel instance, string foo)
        //{
        //    var result = _clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email;
        //    return result != null;
        //    //return !string.IsNullOrEmpty(_clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email);
        //}
    }
}
