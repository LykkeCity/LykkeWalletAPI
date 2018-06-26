using FluentValidation.TestHelper;
using Lykke.WalletApiv2.Tests.Recovery.Validation.TestData;
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

        [TestCaseSource(typeof(StateTokenIsNullOrEmptyTestData))]
        public void Validate_StateTokenIsNullOrEmpty_HaveError(string token, string expectedMessage)
        {
            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(StateTokenIsInvalidTestData))]
        public void Validate_StateTokenIsInvalid_HaveError(string token, string expectedMessage)
        {
            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }        
        
        [TestCaseSource(typeof(StateTokenIsValidTestData))]
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

        [TestCase(null, "\'Pin\' must not be empty.")]
        [TestCase("", "\'Pin\' should not be empty.")]
        public void Validate_PinIsNullOrEmpty_HaveError(string pin, string expectedMessage)
        {
            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Pin, pin);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("abc")]
        [TestCase("123456")]
        public void Validate_PinLengthIsNot4_HaveError(string pin)
        {
            // Arrange
            var expectedMessage = $"'Pin' must be 4 characters in length. You entered {pin.Length} characters.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Pin, pin);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("abcd")]
        [TestCase("/*-+")]
        [TestCase("123a")]
        public void Validate_PinLengthIs4ButNotDigits_HaveError(string pin)
        {
            // Arrange
            const string expectedMessage = "Pin should contain only digits.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Pin, pin);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("1234")]
        public void Validate_PinIsValid_NotHaveError(string pin)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.Pin, pin);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validate_HintIsNullOrEmpty_HaveError(string hint)
        {
            // Arrange
            const string expectedMessage = "Hint should not be empty";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Hint, hint);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("abc-d")]
        [TestCase("a!@bcd#e")]
        public void Validate_HintContainsRestrictedCharacters_HaveError(string hint)
        {
            // Arrange
            const string expectedMessage = "Hint should not contain any special characters.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Hint, hint);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }        

        [TestCase("123456789123456789123456789123456789123456789123456789123456789123456789123456789123456789123456789123456789123456789123456789123")]
        public void Validate_HintLengthIsMoreThan128_HaveError(string hint)
        {
            // Arrange
            const string expectedMessage = "The length of 'Hint' must be 128 characters or fewer. You entered 129 characters.";

            // Act
            var result =  _validator.ShouldHaveValidationErrorFor(model =>  model.Hint, hint);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }        
        
        [TestCase("a, b, c d.")]
        public void Validate_HintDoNotContainAnyRestrictedCharacters_NotHaveError(string hint)
        {
           // Assert
            _validator.ShouldNotHaveValidationErrorFor(model =>  model.Hint, hint);
        }
    }
}