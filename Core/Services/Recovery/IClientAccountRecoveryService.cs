using System.Threading.Tasks;
using Core.Domain.Recovery;
using Core.Dto.Recovery;
using Microsoft.AspNetCore.Http;

namespace Core.Services.Recovery
{
    /// <summary>
    ///     Service for all password recovery operations.
    /// </summary>
    public interface IClientAccountRecoveryService
    {
        /// <summary>
        ///     Start password recovery process.
        /// </summary>
        /// <param name="recoveryStartDto">Data necessary for starting password recovery.</param>
        /// <returns>JWE token, containing current recovery state.</returns>
        Task<string> StartRecoveryAsync(RecoveryStartDto recoveryStartDto);

        /// <summary>
        ///     Get current recovery status.
        /// </summary>
        /// <param name="stateToken">Previously generated state token.</param>
        /// <returns>
        ///     Information about current password recovery state.
        /// </returns>
        Task<RecoveryStatus> GetRecoveryStatusAsync(string stateToken);

        /// <summary>
        ///     Submit current recovery challenge.
        /// </summary>
        /// <param name="submitChallengeDto">Data necessary for submitting the current challenge.</param>
        /// <returns>JWE token, containing new recovery state.</returns>
        Task<string> SubmitChallengeAsync(RecoverySubmitChallengeDto submitChallengeDto);

        /// <summary>
        ///     Method for uploading selfie image for recovery process.
        /// </summary>
        /// <param name="image">Image file uploaded by client.</param>
        /// <returns>Blob id from Azure Blob Storage.</returns>
        Task<string> UploadSelfieFileAsync(IFormFile image);

        /// <summary>
        ///     Complete password recovery process.
        /// </summary>
        /// <param name="recoveryCompleteDto">Data necessary for completing password recovery.</param>
        /// <returns>Completed task, if operation was successful.</returns>
        Task CompleteRecoveryAsync(RecoveryCompleteDto recoveryCompleteDto);
    }
}