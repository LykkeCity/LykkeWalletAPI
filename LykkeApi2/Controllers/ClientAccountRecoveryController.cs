using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Constants;
using Core.Exceptions;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccountRecovery.Client;
using Lykke.Service.ClientAccountRecovery.Client.Models.Enums;
using Lykke.Service.ClientAccountRecovery.Client.Models.Recovery;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Recovery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace LykkeApi2.Controllers
{
    /// <summary>
    ///     Service for client account recovery operations.
    /// </summary>
    [Route("api/account/recovery")]
    [Produces("application/json")]
    [ApiController]
    public class ClientAccountRecoveryController : Controller
    {
        private readonly ILog _log;
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IClientAccountRecoveryServiceClient _accountRecoveryServiceClient;

        public ClientAccountRecoveryController(
            ILog log,
            IRequestContext requestContext,
            IClientAccountClient clientAccountClient,
            IClientAccountRecoveryServiceClient accountRecoveryServiceClient
        )
        {
            _log = log;
            _requestContext = requestContext;
            _accountRecoveryServiceClient = accountRecoveryServiceClient;
            _clientAccountClient = clientAccountClient;
        }

        /// <summary>Start client account recovery process.</summary>
        [HttpPost]
        [Route("start")]
        [ProducesResponseType(typeof(RecoveryStartResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> StartRecovery([FromBody] RecoveryStartRequestModel model)
        {
            try
            {
                var client = await _clientAccountClient.GetClientByEmailAndPartnerIdAsync(model.Email, model.PartnerId);

                if (client == null) throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.ClientNotFound);

                var newRecoveryRequest = new NewRecoveryRequest
                {
                    ClientId = client.Id,
                    Ip = _requestContext.GetIp(),
                    UserAgent = _requestContext.UserAgent
                };

                var newRecoveryResponse =
                    await _accountRecoveryServiceClient.RecoveryApi.StartNewRecoveryAsync(newRecoveryRequest);

                var response = new RecoveryStartResponseModel
                {
                    StateToken = newRecoveryResponse.StateToken
                };

                return Ok(response);
            }
            catch (ClientApiException e)
            {
                _log.WriteWarningAsync(
                    nameof(ClientAccountRecoveryController),
                    nameof(StartRecovery),
                    "Unable to start recovery.",
                    e);

                switch (e.HttpStatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidData);
                    case HttpStatusCode.Forbidden:
                        throw LykkeApiErrorException.Forbidden(LykkeApiErrorCodes.Service
                            .RecoveryStartAttemptLimitReached);
                    default:
                        throw LykkeApiErrorException.InternalServerError(LykkeApiErrorCodes.Service.SomethingWentWrong);
                }
            }
        }

        /// <summary>Get current recovery status.</summary>
        [HttpGet]
        [Route("status")]
        [ProducesResponseType(typeof(RecoveryStatusResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecoveryStatus([FromQuery] RecoveryStatusRequestModel model)
        {
            try
            {
                var request = new RecoveryStatusRequest
                {
                    StateToken = model.StateToken
                };

                var recoveryStatusResponse =
                    await _accountRecoveryServiceClient.RecoveryApi.GetRecoveryStatusAsync(request);

                var response = new RecoveryStatusResponseModel
                {
                    Challenge = recoveryStatusResponse.Challenge.ToString(),
                    OverallProgress = recoveryStatusResponse.OverallProgress.ToString(),
                    ChallengeInfo = recoveryStatusResponse.ChallengeInfo
                };

                return Ok(response);
            }
            catch (ClientApiException e)
            {
                _log.WriteWarningAsync(
                    nameof(ClientAccountRecoveryController),
                    nameof(GetRecoveryStatus),
                    "Unable to get recovery status.",
                    e);

                if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidData);

                throw LykkeApiErrorException.InternalServerError(LykkeApiErrorCodes.Service.SomethingWentWrong);
            }
        }

        /// <summary>Submit challenge to continue recovery process.</summary>
        [HttpPost]
        [Route("challenge")]
        [ProducesResponseType(typeof(RecoverySubmitChallengeResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitChallenge([FromBody] RecoverySubmitChallengeRequestModel model)
        {
            try
            {
                var challengeRequest = new ChallengeRequest
                {
                    StateToken = model.StateToken,
                    Action = model.Action.ParseEnum<Action>(),
                    Value = model.Value ?? string.Empty,
                    Ip = _requestContext.GetIp(),
                    UserAgent = _requestContext.UserAgent
                };

                var challengeResponse =
                    await _accountRecoveryServiceClient.RecoveryApi.SubmitChallengeAsync(challengeRequest);

                if (challengeResponse.OperationStatus.Error)
                    throw LykkeApiErrorException.BadRequest(
                        LykkeApiErrorCodes.Service.RecoverySubmitChallengeInvalidValue,
                        challengeResponse.OperationStatus.Message);

                var response = new RecoverySubmitChallengeResponseModel
                {
                    StateToken = challengeResponse.StateToken
                };

                return Ok(response);
            }
            catch (ClientApiException e)
            {
                _log.WriteWarningAsync(
                    nameof(ClientAccountRecoveryController),
                    nameof(SubmitChallenge),
                    $"Unable to submit challenge. Action: {model.Action}",
                    e);

                if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(
                        LykkeApiErrorCodes.Service.RecoverySubmitChallengeInvalidValue);

                throw LykkeApiErrorException.InternalServerError(LykkeApiErrorCodes.Service.SomethingWentWrong);
            }
        }

        /// <summary>Upload selfie file.</summary>
        [HttpPost]
        [Route("file")]
        [ProducesResponseType(typeof(RecoveryUploadFileResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SelfieMaxSize]
        public async Task<IActionResult> UploadFile([FromForm] RecoveryUploadFileRequestModel model)
        {
            var file = model.File;

            try
            {
                using (var imageStream = file.OpenReadStream())
                {
                    var streamPart = new StreamPart(imageStream, file.FileName, file.ContentType);

                    var selfieResponse =
                        await _accountRecoveryServiceClient.RecoveryApi.UploadSelfieAsync(model.StateToken, streamPart);

                    var response = new RecoveryUploadFileResponseModel
                    {
                        FileId = selfieResponse.FileId
                    };

                    return Ok(response);
                }
            }
            catch (ClientApiException e)
            {
                _log.WriteWarningAsync(
                    nameof(ClientAccountRecoveryController),
                    nameof(UploadFile),
                    $"Unable to upload file. FileName: {file.FileName}; Length: {file.Length} bytes; ContentType: {file.ContentType};",
                    e);

                if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidData, e.Message);

                throw LykkeApiErrorException.InternalServerError(LykkeApiErrorCodes.Service.SomethingWentWrong);
            }
        }

        /// <summary>Complete recovery by setting new password.</summary>
        [HttpPost]
        [Route("password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Complete([FromBody] RecoveryCompleteRequestModel model)
        {
            try
            {
                var passwordRequest = new PasswordRequest
                {
                    StateToken = model.StateToken,
                    PasswordHash = model.PasswordHash,
                    Pin = model.Pin,
                    Hint = model.Hint,
                    Ip = _requestContext.GetIp(),
                    UserAgent = _requestContext.UserAgent
                };

                await _accountRecoveryServiceClient.RecoveryApi.UpdatePasswordAsync(passwordRequest);

                return Ok();
            }
            catch (ClientApiException e)
            {
                _log.WriteWarningAsync(
                    nameof(ClientAccountRecoveryController),
                    nameof(Complete),
                    "Unable to complete recovery.",
                    e);

                if (e.HttpStatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidData);

                throw LykkeApiErrorException.InternalServerError(LykkeApiErrorCodes.Service.SomethingWentWrong);
            }
        }
    }
}