using FluentValidation.TestHelper;
using Lykke.WalletApiv2.Tests.Recovery.Validation.TestData;
using LykkeApi2.Validation.Recovery;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation
{
    [TestFixture]
    [SetCulture("en")]
    internal class RecoveryUploadFileRequestModelValidatorTests
    {
        [SetUp]
        public void Setup()
        {
            _validator = new RecoveryUploadFileRequestModelValidator();
        }

        private RecoveryUploadFileRequestModelValidator _validator;

        [TestCaseSource(typeof(SelfieFileInvalidSignatureTestData))]
        public void Validate_SelfieFileInvalidSignature_HaveError(IFormFile file)
        {
            // Arrange
            const string expectedMessage =
                "File should be an image. Allowed image formats: .jpg, .jpeg, .png, .gif, .bmp.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.File, file);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(SelfieFileInvalidExtensionTestData))]
        public void Validate_SelfieFileInvalidExtension_HaveError(IFormFile file)
        {
            // Arrange
            const string expectedMessage =
                "File should have valid extension. Allowed extensions: .jpg, .jpeg, .png, .gif, .bmp.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.File, file);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(SelfieFileNameIsNullOrEmptyTestData))]
        public void Validate_SelfieFileNameIsNullOrEmpty_HaveError(IFormFile file)
        {
            // Arrange
            const string expectedMessage = "File name should not be empty.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.File, file);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(SelfieFileStreamIsNullTestData))]
        public void Validate_SelfieFileStreamIsNull_HaveError(IFormFile file)
        {
            // Arrange
            const string expectedMessage = "File should contain data.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.File, file);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(SelfieFileStreamIsTooShortTestData))]
        public void Validate_SelfieFileStreamIsTooShort_HaveError(IFormFile file)
        {
            // Arrange
            const string expectedMessage = "File content should not be too short.";

            // Act
            var result = _validator.ShouldHaveValidationErrorFor(model => model.File, file);

            // Assert
            result.WithErrorMessage(expectedMessage);
        }

        [TestCaseSource(typeof(SelfieFileIsValidTestData))]
        public void Validate_ValidFile_NotHaveError(IFormFile file)
        {
            // Assert
            _validator.ShouldNotHaveValidationErrorFor(model => model.File, file);
        }
    }
}