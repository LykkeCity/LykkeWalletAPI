using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Constants;
using Lykke.Common.ApiLibrary.Contract;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Constants
{
    [TestFixture]
    public class LykkeApiErrorCodesTests
    {
        private readonly HashSet<string> _expectedErrorCodes = new HashSet<string>
        {
            "ClientNotFound",
            "AssetNotFound",
            "AssetUnavailable",
            "CountryUnavailable",
            "PendingDialogs",
            "BlockchainWalletDepositAddressNotGenerated",
            "BlockchainWalletDepositAddressAlreadyGenerated",
            "KycRequired",
            "DepositLimitReached",
            "ModelValidationFailed",
            "InvalidInput",
            "InconsistentState",
            "TwoFactorRequired",
            "SecondFactorCodeIncorrect",
            "SecondFactorCheckForbiden",
            "SecondFactorSetupInProgress",
            "SecondFactorAlreadySetup",
            "MaxAttemptsReached",
            "AddressAlreadyWhitelisted",
            "WhitelistingError",
            "WhitelistingDeleteError",
            "SecondFactorIsNotSetup"
        };

        /// <summary>
        ///     Ensures each code is unique and it's value has not changed.
        ///     Verifies that newly added codes has test cases.
        ///     Verifies if codes were removed their test cases is removed too.
        ///     If for some reasons you have modified error codes contract,
        ///     please fix unit test cases too. This is needed to make sure you have changed error code knowingly.
        /// </summary>
        [Test]
        public void ErrorCodes_WasNotModifiedAccidently()
        {
            var currentErrorCodes = GetAllCurrentApiErrorCodes(typeof(LykkeApiErrorCodes));

            foreach (var expectedCode in _expectedErrorCodes)
            {
                if (!currentErrorCodes.Contains(expectedCode))
                    Assert.Fail(
                    $"Error code: \"{expectedCode}\" was removed! But it still have a test. If you removed it knowingly please remove it from {nameof(_expectedErrorCodes)}.");
            }

            if (currentErrorCodes.Count > _expectedErrorCodes.Count)
            {
                var addedErrorCodes = currentErrorCodes.Except(_expectedErrorCodes);

                foreach (var addedErrorCode in addedErrorCodes)
                    Assert.Fail(
                        $"Code: \"{addedErrorCode}\" was added, but don't have a test. Please add it to {nameof(_expectedErrorCodes)}.");
            }
        }

        /// <summary>
        /// Get all current ErrorCodes defined in <see cref="LykkeApiErrorCodes"/>
        /// </summary>
        private static HashSet<string> GetAllCurrentApiErrorCodes(Type type,
            HashSet<string> errorCodeNames = null)
        {
            if (errorCodeNames == null)
                errorCodeNames = new HashSet<string>();

            var errorCodes = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var propertyInfo in errorCodes)
            {
                var errorCodeKey = propertyInfo.Name;

                var errorCode = propertyInfo.GetValue(null) as ILykkeApiErrorCode;

                if (errorCode == null)
                    Assert.Fail($"Error code: \"{errorCodeKey}\" is null!");

                var errorCodeName = errorCode.Name;

                if(!string.Equals(errorCodeKey, errorCodeName))
                    Assert.Fail($"Error code: \"{errorCodeKey}\" name should match field name!");

                if(!errorCodeNames.Add(errorCodeName))
                    Assert.Fail($"Error code: \"{errorCodeName}\" should have unique name!");
            }

            var typeGroups = type.GetNestedTypes();

            foreach (var typeGroup in typeGroups) GetAllCurrentApiErrorCodes(typeGroup, errorCodeNames);

            return errorCodeNames;
        }
    }
}
