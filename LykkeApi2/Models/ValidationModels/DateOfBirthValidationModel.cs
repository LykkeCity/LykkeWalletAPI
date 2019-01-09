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
        private static readonly DateTime MinDate = new DateTime(1900, 1, 1);

        public DateOfBirthValidationModel()
        {
            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.AddYears(-AgeRestriction))
                .WithMessage($"Age is required to be {AgeRestriction}+")
                .GreaterThanOrEqualTo(MinDate)
                .WithMessage($"Date of birth is required to be greater or equal to {MinDate.ToShortDateString()}");
        }
    }
}
