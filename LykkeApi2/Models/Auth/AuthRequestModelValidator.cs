using Core.Constants;
using FluentValidation;
using LykkeApi2.Strings;

namespace LykkeApi2.Models.Auth
{
    public class AuthRequestModelValidator : AbstractValidator<AuthRequestModel>
    {
        public AuthRequestModelValidator()
        {
            RuleFor(m => m.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(m => m.Password)
                .NotEmpty()
                .Length(LykkeConstants.MinPwdLength, LykkeConstants.MaxPwdLength);
        }   
    }
}