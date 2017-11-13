using FluentValidation;
using LykkeApi2.Models.OperationsDetailsModels;

namespace LykkeApi2.Models.ValidationModels.OperationsDetailsModels
{
    public class OperationsDetailsValidationModel : AbstractValidator<OperationsDetailsModel>
    {
        public OperationsDetailsValidationModel()
        {
            RuleFor(op => op.Comment).NotNull();
            RuleFor(op => op.Comment).NotEmpty();
            RuleFor(op => op.TransactionId).NotNull();
            RuleFor(op => op.TransactionId).NotEmpty();
        }
    }
}
