using Lykke.Common.ApiLibrary.Contract;
using Lykke.Common.ApiLibrary.Exceptions;

namespace Core.Constants
{
    /// <summary>
    ///     Class for storing all possible error codes that may happen in Api.
    ///     Use it with <see cref="LykkeApiErrorException" />.
    /// </summary>
    public static class LykkeApiErrorCodes
    {
        /// <summary>
        ///     Group for client and service related error codes.
        /// </summary>
        public static class Service
        {
            /// <summary>
            ///     One of the provided values was not valid.
            /// </summary>
            public static readonly ILykkeApiErrorCode InvalidInput =
                new LykkeApiErrorCode(nameof(InvalidInput), "One of the provided values was not valid.");

            /// <summary>
            ///     Client was not found.
            /// </summary>
            public static readonly ILykkeApiErrorCode ClientNotFound =
                new LykkeApiErrorCode(nameof(ClientNotFound), "Client not found.");

            /// <summary>
            ///     The requested asset was not found.
            /// </summary>
            public static readonly ILykkeApiErrorCode AssetNotFound =
                new LykkeApiErrorCode(nameof(AssetNotFound), "The requested asset was not found.");

            /// <summary>
            ///     The requested asset is unavailable for the current action.
            /// </summary>
            public static readonly ILykkeApiErrorCode AssetUnavailable =
                new LykkeApiErrorCode(nameof(AssetUnavailable),
                    " The requested asset is unavailable for the current action.");

            /// <summary>
            ///     The requested country is unavailable for the current action.
            /// </summary>
            public static readonly ILykkeApiErrorCode CountryUnavailable =
                new LykkeApiErrorCode(nameof(CountryUnavailable),
                    " The requested country is unavailable for the current action.");

            /// <summary>
            ///     One or more dialogs need to be confirmed before the current action.
            /// </summary>
            public static readonly ILykkeApiErrorCode
                PendingDialogs = new LykkeApiErrorCode(nameof(PendingDialogs),
                    "One or more dialogs need to be confirmed before the current action.");

            /// <summary>
            ///     The deposit address is not generated.
            /// </summary>
            public static readonly ILykkeApiErrorCode BlockchainWalletDepositAddressNotGenerated =
                new LykkeApiErrorCode(nameof(BlockchainWalletDepositAddressNotGenerated),
                    "The deposit address is not generated.");

            public static readonly ILykkeApiErrorCode BlockchainWalletDepositAddressAlreadyGenerated =
                new LykkeApiErrorCode(nameof(BlockchainWalletDepositAddressAlreadyGenerated),
                    "The address is already generated.");

            /// <summary>
            ///     The client's KYC level is insufficient for the current action.
            /// </summary>
            public static readonly ILykkeApiErrorCode KycRequired =
                new LykkeApiErrorCode(nameof(KycRequired),
                    "The client's KYC level is insufficient for the current action.");

            /// <summary>
            ///     The deposit limit is reached.
            /// </summary>
            public static readonly ILykkeApiErrorCode DepositLimitReached =
                new LykkeApiErrorCode(nameof(DepositLimitReached), "The deposit limit is reached.");

            /// <summary>
            ///     The deposit limit is reached.
            /// </summary>
            public static readonly ILykkeApiErrorCode InconsistentState =
                new LykkeApiErrorCode(nameof(InconsistentState), "The call was unexpected.");

            /// <summary>
            ///     Two factor authentication should be enabled.
            /// </summary>
            public static readonly ILykkeApiErrorCode TwoFactorRequired =
                new LykkeApiErrorCode(nameof(TwoFactorRequired), "The action requires 2fa enabled.");

            /// <summary>
            ///     Two factor authentication code is incorrect.
            /// </summary>
            public static readonly ILykkeApiErrorCode SecondFactorCodeIncorrect =
                new LykkeApiErrorCode(nameof(SecondFactorCodeIncorrect), "The provided code for 2FA is incorrect.");

            /// <summary>
            ///     Two factor authentication check forbidden.
            /// </summary>
            public static readonly ILykkeApiErrorCode SecondFactorCheckForbiden =
                new LykkeApiErrorCode(nameof(SecondFactorCheckForbiden), "2FA check forbidden.");

            /// <summary>
            ///     Two factor verification is already in the progress should be enabled.
            /// </summary>
            public static readonly ILykkeApiErrorCode SecondFactorSetupInProgress =
                new LykkeApiErrorCode(nameof(SecondFactorSetupInProgress), "2FA setup is in progress.");

            /// <summary>
            ///     Two factor authentication already setup.
            /// </summary>
            public static readonly ILykkeApiErrorCode SecondFactorAlreadySetup =
                new LykkeApiErrorCode(nameof(SecondFactorAlreadySetup), "2FA already setup.");

            /// <summary>
            ///     Max number of attempts reached
            /// </summary>
            public static readonly ILykkeApiErrorCode MaxAttemptsReached =
                new LykkeApiErrorCode(nameof(MaxAttemptsReached), "Maximum attempts reached for this call.");

            /// <summary>
            ///     Address has already been whitelisted
            /// </summary>
            public static readonly ILykkeApiErrorCode AddressAlreadyWhitelisted =
                new LykkeApiErrorCode(nameof(AddressAlreadyWhitelisted), "Address has already been whitelisted.");

            /// <summary>
            ///     Error whitelisting address
            /// </summary>
            public static readonly ILykkeApiErrorCode WhitelistingError =
                new LykkeApiErrorCode(nameof(WhitelistingError), "Address wasn't whitelisted.");

            /// <summary>
            ///     Error delete whitelisting item
            /// </summary>
            public static readonly ILykkeApiErrorCode WhitelistingDeleteError =
                new LykkeApiErrorCode(nameof(WhitelistingDeleteError), "Whitelisting item wasn't deleted..");
        }

        /// <summary>
        ///     Group for all model validation error codes.
        /// </summary>
        public static class ModelValidation
        {
            /// <summary>
            ///     Common error code for any failed validation.
            ///     Use it as default validation error code if specific code is not required.
            /// </summary>
            public static readonly ILykkeApiErrorCode ModelValidationFailed =
                new LykkeApiErrorCode(nameof(ModelValidationFailed), "The model is invalid.");
        }
    }
}
