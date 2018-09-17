using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Model for uploading recovery selfie image.
    /// </summary>
    public class RecoveryUploadFileRequestModel
    {
        /// <summary>
        ///     JWE token containing current state of recovery process.
        /// </summary>
        [FromQuery]
        public string StateToken { get; set; }

        // Note: swashbuckle incorrectly interprets File as query param instead of FormData.
        /// <summary>
        ///     File to upload as selfie.
        /// </summary>
        [FromForm]
        public IFormFile File { get; set; }
    }
}