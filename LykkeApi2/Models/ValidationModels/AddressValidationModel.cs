using FluentValidation;
using JetBrains.Annotations;
using LykkeApi2.Models.Client;
using LykkeApi2.Services;

namespace LykkeApi2.Models.ValidationModels
{
    [UsedImplicitly]
    public class AddressValidationModel : AbstractValidator<AddressModel>
    {
        private const int UnitedKingdomAddressMaxLength = 32;

        public AddressValidationModel(KycCountryValidator countryValidator)
        {
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required");

            When(x => countryValidator.IsUnitedKingdom(), () =>
            {
                RuleFor(x => x.Address)
                    .MaximumLength(UnitedKingdomAddressMaxLength)
                    .WithMessage($"Address in United Kingdom is limited to {UnitedKingdomAddressMaxLength} characters");
            });
        }
    }
}