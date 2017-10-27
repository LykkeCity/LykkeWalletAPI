using FluentValidation;
using LykkeApi2.Models.TransactionHistoryModels;

namespace LykkeApi2.Models.ValidationModels.OperationsDetailsModels
{
    public class OperationsDetailHistoryRequestValidationModel : AbstractValidator<OperationsDetailHistoryRequestModel>
    {
        public OperationsDetailHistoryRequestValidationModel()
        {
            RuleFor(op => op.ClientId).NotNull();
            RuleFor(op => op.ClientId).NotEmpty();
            RuleFor(op => op.TransactionId).NotNull();
            RuleFor(op => op.TransactionId).NotEmpty();
        }
    }
}
