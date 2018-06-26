using FluentValidation.TestHelper;
using Lykke.WalletApiv2.Tests.Recovery.Validation.TestData;
using LykkeApi2.Validation.Recovery;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation
{
    [TestFixture]
    [SetCulture("en")]
    public class RecoverySubmitChallengeRequestModelValidatorTests
    {
        private RecoverySubmitChallengeRequestModelValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new RecoverySubmitChallengeRequestModelValidator();
        }

        [TestCaseSource(typeof(StateTokenIsNullOrEmptyTestData))]
        public void Validate_StateTokenIsNullOrEmpty_HaveError(string token, string expectedMessage)
        {
            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(StateTokenIsInvalidTestData))]
        public void Validate_StateTokenIsInvalid_HaveError(string token, string expectedMessage)
        {
            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.StateToken, token);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(StateTokenIsValidTestData))]
        public void Validate_StateTokenIsValid_NotHaveError(string token)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.StateToken, token);
        }

        [TestCase("")]
        public void Validate_ValueIsEmpty_NotHaveError(string value)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.Value, value);
        }

        [TestCase("a!@bcd#e")]
        [TestCase("a, b, c d.")]
        public void Validate_ValueContainsRestrictedCharacters_HaveError(string value)
        {
            // Arrange
            const string expectedMessage = "Value should not contain any special characters.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.Value, value);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("1234")]
        [TestCase("a2934fa2-6f7e-4ac9-8210-681814ac86c4")]
        [TestCase("105c681eee4b4a6fbb6591dd9fd5800d.png")]
        public void Validate_ValueDoNotContainAnyRestrictedCharacters_NotHaveError(string value)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.Value, value);
        }
    }
}