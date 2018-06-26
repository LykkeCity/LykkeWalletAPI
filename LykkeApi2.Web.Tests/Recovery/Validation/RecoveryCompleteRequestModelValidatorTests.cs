using FluentValidation.TestHelper;
using LykkeApi2.Validation.Recovery;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation
{
    [TestFixture]
    [SetCulture("en")]
    public class RecoveryCompleteRequestModelValidatorTests
    {

        private  RecoveryCompleteRequestModelValidator _validator;

        [SetUp]
        public void Setup() {
            _validator = new  RecoveryCompleteRequestModelValidator();
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validate_StateTokenIsNullOrEmpty_HaveError(string token)
        {
            // Arrange
            const string expectedMessage = "State Token should not be empty";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("abcd")]
        [TestCase("12345")]
        public void Validate_StateTokenIsInvalid_HaveError(string token)
        {
            // Arrange
            const string expectedMessage = "State Token should be a valid JWE token.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }        
        
        [TestCase("a1.b2.c3.d4.e5")]
        [TestCase("a1..c3.d4.e5")]
        public void Validate_StateTokenIsValid_NotHaveError(string token)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model =>  model.StateToken, token);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validate_PasswordHashIsNullOrEmpty_HaveError(string hash)
        {
            // Arrange
            const string expectedMessage = "Password Hash should not be empty";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.PasswordHash, hash);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("hello world")]
        [TestCase("1234")]
        [TestCase("03AC674216F3E15C761EE1A5E255")]
        public void Validate_PasswordHashIsInvalid_HaveError(string hash)
        {
            // Arrange
            const string expectedMessage = "Password Hash should be a valid sha256 hash.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.PasswordHash, hash);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9")]
        [TestCase("03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4")]
        public void Validate_PasswordHashIsValid_NotHaveError(string hash)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model =>  model.PasswordHash, hash);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validate_PinIsNullOrEmpty_HaveError(string pin)
        {
            // Arrange
            const string expectedMessage = "\'Pin\' should not be empty.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Pin, pin);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("abc")]
        [TestCase("123456")]
        public void Validate_PinIsInvalid_HaveError(string pin)
        {
            // Arrange
            // TODO:@gafanasiev think about single quotes
            var expectedMessage = $"'Pin' must be 4 characters in length. You entered {pin.Length} characters.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Pin, pin);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        //RuleFor(x => x.Pin)
        //    .Length(4)
        //    .Custom(OnlyDigitsFluentValidator.Validate);
        //RuleFor(x => x.Hint)
        //    .MaximumLength(LykkeConstants.MaxFieldLength)
        //    .Custom(_hintValidator.Validate);
    }
}