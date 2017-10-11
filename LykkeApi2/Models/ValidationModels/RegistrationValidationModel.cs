using FluentValidation;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ValidationModels.RegistrationValidations;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels
{
    public class RegistrationValidationModel : AbstractValidator<AccountRegistrationModel>
    {        
        private readonly IClientAccountService _clientAccountService;
        
        public RegistrationValidationModel(IClientAccountService clientAccountService)
        {            
            _clientAccountService = clientAccountService;
            
            RuleFor(reg => reg.Email).NotNull();
            RuleFor(reg => reg.Hint).ValidHintVlue();
            RuleFor(reg => reg.Email).Must(IsEmaiVerified).WithMessage(Phrases.EmailNotVerified);
            //RuleFor(reg => reg.Email).Must(IsTraderWithEmailExistsForPartner).WithMessage(Phrases.ClientWithEmailIsRegistered);

        }

        private bool IsEmaiVerified(AccountRegistrationModel instance, string value)
        {            
            return _clientAccountService.IsEmailVerified(new VerifiedEmailModel(instance.Email, instance.PartnerId)) ?? false;
        }

        //private bool IsTraderWithEmailExistsForPartner(AccountRegistrationModel instance, string foo)
        //{
        //    var result = _clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email;
        //    return result != null;
        //    //return !string.IsNullOrEmpty(_clientAccountClient.GetClientByEmailAndPartnerId(instance.Email, instance.PartnerId).Result.Email);
        //}
    }
}
