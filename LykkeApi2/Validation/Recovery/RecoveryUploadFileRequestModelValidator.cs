using FluentValidation;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Common;

namespace LykkeApi2.Validation.Recovery
{
    public class RecoveryUploadFileRequestModelValidator : AbstractValidator<RecoveryUploadFileRequestModel>
    {
        public RecoveryUploadFileRequestModelValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(x => x.File)
                .NotNull()
                .SetValidator(new ImageTypeIFormFileFluentValidator(".jpg", ".jpeg", ".png", ".gif", ".bmp"));
        }
    }
}