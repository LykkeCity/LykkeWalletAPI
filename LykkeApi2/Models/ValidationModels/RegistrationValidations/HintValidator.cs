using Common;
using FluentValidation;
using FluentValidation.Validators;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.ValidationModels.RegistrationValidations
{
    public class HintValidator : PropertyValidator
    {
        public HintValidator() : base(Phrases.InvalidPropertyValue)
        {
        }
        protected override bool IsValid(PropertyValidatorContext context)
        {
            string hint = (string)context.PropertyValue;
            if (!hint.ContainsHtml())
                return true;

            return false;
        }
    }

    public static class HintValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> ValidHintVlue<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new HintValidator());
        }
    }
}
