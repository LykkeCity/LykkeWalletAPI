using FluentValidation;
using JetBrains.Annotations;
using LykkeApi2.Models.Orders;

namespace LykkeApi2.Models.ValidationModels
{
    [UsedImplicitly]
    public class StopLimitOrderRequestValidationModel : AbstractValidator<StopLimitOrderRequest>
    {
        public StopLimitOrderRequestValidationModel()
        {
            RuleFor(x => x.Volume)
                .Must(x => x > 0)
                .WithMessage("{PropertyName} must be > 0");
            
            RuleFor(r => r.AssetPairId)
                .NotEmpty()
                .WithMessage("{PropertyName} should not be empty");
            
            RuleFor(x => x.LowerLimitPrice)
                .Must((request, value) => request.LowerLimitPrice.HasValue && request.LowerPrice.HasValue)
                .When(x => !x.UpperLimitPrice.HasValue && !x.UpperPrice.HasValue)
                .WithMessage("Lower and/or upper limits must be set");
            
            RuleFor(x => x.UpperLimitPrice)
                .Must((request, value) => request.UpperLimitPrice.HasValue && request.UpperPrice.HasValue)
                .When(x => !x.LowerLimitPrice.HasValue && !x.LowerPrice.HasValue)
                .WithMessage("Lower and/or upper limits must be set");
            
            RuleFor(x => x.LowerLimitPrice).NotNull()
                .When(x => x.LowerPrice.HasValue)
                .WithMessage(x => $"{nameof(x.LowerLimitPrice)} must be set with {nameof(x.LowerPrice)}");
            
            RuleFor(x => x.LowerPrice).NotNull()
                .When(x => x.LowerLimitPrice.HasValue)
                .WithMessage(x => $"{nameof(x.LowerPrice)} must be set with {nameof(x.LowerLimitPrice)}");
            
            RuleFor(x => x.UpperLimitPrice).NotNull()
                .When(x => x.UpperPrice.HasValue)
                .WithMessage(x => $"{nameof(x.UpperLimitPrice)} must be set with {nameof(x.UpperPrice)}");
            
            RuleFor(x => x.UpperPrice).NotNull()
                .When(x => x.UpperLimitPrice.HasValue)
                .WithMessage(x => $"{nameof(x.UpperPrice)} must be set with {nameof(x.UpperLimitPrice)}");
            
            RuleFor(x => x.LowerLimitPrice).Must(x => x > 0)
                .When(x => x.LowerLimitPrice.HasValue)
                .WithMessage("{PropertyName} must be > 0");
            
            RuleFor(x => x.LowerPrice).Must(x => x > 0)
                .When(x => x.LowerPrice.HasValue)
                .WithMessage("{PropertyName} must be > 0");
            
            RuleFor(x => x.UpperLimitPrice).Must(x => x > 0)
                .When(x => x.UpperLimitPrice.HasValue)
                .WithMessage("{PropertyName} must be > 0");
            
            RuleFor(x => x.UpperPrice).Must(x => x > 0)
                .When(x => x.UpperPrice.HasValue)
                .WithMessage("{PropertyName} must be > 0");
            
            RuleFor(x => x.UpperLimitPrice)
                .Must((request, value) => request.UpperLimitPrice > request.LowerLimitPrice)
                .When(x => x.UpperLimitPrice.HasValue && x.LowerLimitPrice.HasValue)
                .WithMessage(x => $"{nameof(x.UpperLimitPrice)} must be greater than {nameof(x.LowerLimitPrice)}");
        }
    }
}
