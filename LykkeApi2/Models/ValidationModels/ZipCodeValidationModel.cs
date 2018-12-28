using FluentValidation;
using JetBrains.Annotations;
using LykkeApi2.Models.Client;
using LykkeApi2.Services;

namespace LykkeApi2.Models.ValidationModels
{
    [UsedImplicitly]
    public class ZipCodeValidationModel : AbstractValidator<ZipCodeModel>
    {
        public ZipCodeValidationModel(KycCountryValidator countryValidator)
        {
            RuleFor(x => x.Zip)
                .NotEmpty()
                .WithMessage("Zip code is required");

            When(x => countryValidator.IsUnitedKingdom(), () =>
            {
                RuleFor(x => x.Zip)
                    .Matches(@"^[A-Z0-9]{3}\s[A-Z0-9]{3}$")
                    .WithMessage("Postal code in United Kingdom must be in the following format: SW4 6EH");
            });
        }   
    }
}