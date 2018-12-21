using System;
using FluentValidation;
using JetBrains.Annotations;
using LykkeApi2.Models.Client;

namespace LykkeApi2.Models.ValidationModels
{
    [UsedImplicitly]
    public class DateOfBirthValidationModel : AbstractValidator<DateOfBirthModel>
    {
        private const int AgeRestriction = 18;

        public DateOfBirthValidationModel()
        {
            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.AddYears(-AgeRestriction))
                .WithMessage($"Age is required to be {AgeRestriction}+");
        }
    }
}