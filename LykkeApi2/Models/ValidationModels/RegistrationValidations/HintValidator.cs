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
            string hint = (string) context.PropertyValue;

            return string.IsNullOrWhiteSpace(hint) ? true : hint.ContainsHtml() ? false : true;
        }
    }

    public static class HintValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> ValidHintValue<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new HintValidator());
        }
    }
}