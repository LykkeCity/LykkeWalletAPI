using FluentValidation.TestHelper;
using LykkeApi2.Validation.Recovery;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation
{
    [TestFixture]
    [SetCulture("en")]
    internal class RecoveryStartRequestModelValidatorTests
    {
        private RecoveryStartRequestModelValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new RecoveryStartRequestModelValidator();
        }

        [TestCase(null, "\'Email\' must not be empty.")]
        [TestCase("", "\'Email\' should not be empty.")]
        public void Validate_EmailIsNullOrEmpty_HaveError(string email, string expectedMessage)
        {
            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.Email, email);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("test@test")]
        [TestCase("test@123,com")]
        [TestCase("test")]
        public void Validate_EmailHasWrongFormat_HaveError(string email)
        {
            // Arrange
            const string expectedMessage = "\'Email\' is not a valid email address.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.Email, email);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("test@test.com")]
        public void Validate_EmailIsValid_NotHaveError(string email)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.Email, email);
        }

        [TestCase("")]
        public void Validate_PartnerIdIsEmpty_NotHaveError(string partnerId)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.PartnerId, partnerId);
        }

        [TestCase("a!@bcd#e")]
        public void Validate_PartnerIdContainsRestrictedCharacters_HaveError(string partnerId)
        {
            // Arrange
            const string expectedMessage = "Partner Id should not contain any special characters.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.PartnerId, partnerId);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCase("a2934fa2-6f7e-4ac9-8210-681814ac86c4")]
        public void Validate_PartnerIdDoNotContainAnyRestrictedCharacters_NotHaveError(string partnerId)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.PartnerId, partnerId);
        }
    }
}