using System;
using System.Collections.Generic;
using Common;
using FluentValidation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using LykkeApi2.Models.Kyc;

namespace LykkeApi2.Models.ValidationModels
{
    public class KycAdditionalPersonalInfoValidationModel : AbstractValidator<KycAdditionalPersonalInfoModel>
    {
        #region consts
        private const int AgeRestriction = 18;
        private const string UnitedKingdomIso3Code = "GBR";
        private const int UnitedKingdomStreetNameMaxLength = 32;
        #endregion

        private static readonly List<KycStatus> AcceptedKycStatuses = new List<KycStatus>
            {KycStatus.NeedToFillData, KycStatus.Pending};

        public KycAdditionalPersonalInfoValidationModel()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("Client id is required");

            RuleFor(x => x.Country)
                .NotEmpty()
                .WithMessage("Country is required")
                .Must(CountryManager.HasIso3)
                .WithMessage("Country code must be in iso3 format");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("Date of birth is required")
                .LessThan(DateTime.UtcNow.AddYears(-AgeRestriction))
                .WithMessage($"Age is required to be {AgeRestriction}+");

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required");

            RuleFor(x => x.Zip)
                .NotEmpty()
                .WithMessage("Zip code is required");

            RuleFor(x => x.KycStatus)
                .Must(x => AcceptedKycStatuses.Contains(x))
                .WithMessage("Client account kyc status is not valid for posting additional data");

            When(x => x.Country.Equals(UnitedKingdomIso3Code), () =>
            {
                RuleFor(x => x.Address)
                    .MaximumLength(UnitedKingdomStreetNameMaxLength)
                    .WithMessage($"Street name in United Kingdom is limited to {UnitedKingdomStreetNameMaxLength} characters");

                RuleFor(x => x.Zip)
                    .Matches(@"^[A-Z0-9]{3}\s[A-Z0-9]{3}$")
                    .WithMessage("Postal code in United Kingdom must be in the following format: SW4 6EH");
            });
        }
    }
}