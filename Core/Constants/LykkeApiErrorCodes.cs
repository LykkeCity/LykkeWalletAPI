using Core.Domain.LykkeApiError;
using Core.Exceptions;

namespace Core.Constants
{
    /// <summary>
    ///     Class for storing all possible error codes that may happen in Api.
    ///     Use it with <see cref="LykkeApiErrorException" />.
    /// </summary>
    public static partial class LykkeApiErrorCodes
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
                new LykkeApiErrorCode(nameof(InconsistentState), "The action requires 2fa enabled.");

            ///     Recovery completion process failed because of invalid data.
            /// </summary>
            public static readonly ILykkeApiErrorCode RecoveryCompleteFailedInvalidData =
                new LykkeApiErrorCode(nameof(RecoveryCompleteFailedInvalidData),
                    "Can not complete the recovery, invalid data was passed.");

            /// <summary>
            ///     Recovery start process is forbidden because recovery attempt limit reached.
            /// </summary>
            public static readonly ILykkeApiErrorCode RecoveryStartAttemptLimitReached =
                new LykkeApiErrorCode(nameof(RecoveryStartAttemptLimitReached),
                    "Can not start a new recovery process, because recovery attempt limit is reached.");

            /// <summary>
            ///     User tried to upload invalid image file during selfie verification challenge.
            /// </summary>
            public static readonly ILykkeApiErrorCode RecoveryUploadInvalidSelfieFile =
                new LykkeApiErrorCode(nameof(RecoveryUploadInvalidSelfieFile), "Invalid image file.");

            /// <summary>
            ///     Invalid value was submitted to challenge.
            /// </summary>
            public static readonly ILykkeApiErrorCode RecoverySubmitChallengeInvalidValue =
                new LykkeApiErrorCode(nameof(RecoverySubmitChallengeInvalidValue),
                    "An invalid value was submitted to the challenge.");
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