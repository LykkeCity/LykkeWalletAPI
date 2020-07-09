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
        }
    }
}
