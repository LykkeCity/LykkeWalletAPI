using FluentValidation.TestHelper;
using Lykke.WalletApiv2.Tests.Recovery.Validation.TestData;
using LykkeApi2.Validation.Recovery;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation
{
    [TestFixture]
    [SetCulture("en")]
    public class RecoveryStatusRequestModelValidatorTests
    {
        private RecoveryStatusRequestModelValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new RecoveryStatusRequestModelValidator();
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
    }
}