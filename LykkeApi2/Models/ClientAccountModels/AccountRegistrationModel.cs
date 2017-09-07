using Core.Constants;
using FluentValidation;
using FluentValidation.Validators;
using LykkeApi2.Strings;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.ClientAccountModels
{

    public class AccountRegistrationModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FullName { get; set; }

        public string ContactPhone { get; set; }

        [Required]
        [MaxLength(LykkeConstants.MaxPwdLength)]
        [MinLength(LykkeConstants.MinPwdLength)]
        public string Password { get; set; }

        public string Hint { get; set; }

        public string ClientInfo { get; set; }

        public string PartnerId { get; set; }
    }

   
    public class CustomerTypeValidator : PropertyValidator
    {
        public CustomerTypeValidator() : base("Customer type {PropertyName} is not a valid type :D")
        {
        }
        protected override bool IsValid(PropertyValidatorContext context)
        {
            string customerType = (string)context.PropertyValue;
            if (customerType.ToLower() == "person" || customerType.ToLower() == "company")
                return true;

            return false;
        }
    }

    public static class CustomValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> ValidCustomerType<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new CustomerTypeValidator());
        }
    }

}
