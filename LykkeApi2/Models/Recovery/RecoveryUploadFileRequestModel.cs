using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Model for uploading recovery selfie image.
    /// </summary>
    public class RecoveryUploadFileRequestModel
    {
        /// <summary>
        ///     File to upload as selfie.
        /// </summary>
        public IFormFile File { get; set; }
    }
}