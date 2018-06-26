using System.Threading.Tasks;
using AutoMapper;
using Core.Domain.Recovery;
using Core.Dto.Recovery;
using Core.Services.Recovery;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Recovery;
using LykkeApi2.Validation.Recovery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IClientAccountRecoveryService _clientAccountRecoveryService;
        private readonly IMapper _mapper;
        private readonly IRequestContext _requestContext;

        public ClientAccountRecoveryController(
            IMapper mapper,
            IRequestContext requestContext,
            IClientAccountRecoveryService clientAccountRecoveryService)
        {
            _mapper = mapper;
            _requestContext = requestContext;
            _clientAccountRecoveryService = clientAccountRecoveryService;
        }

        /// <summary>Start client account recovery process.</summary>
        [HttpPost]
        [Route("start")]
        [ProducesResponseType(typeof(RecoveryStartResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> StartRecovery([FromBody] RecoveryStartRequestModel model)
        {
            var startRecoveryDto = _mapper.Map<RecoveryStartRequestModel, RecoveryStartDto>(model,
                opt =>
                {
                    opt.Items["Ip"] = _requestContext.GetIp();
                    opt.Items["UserAgent"] = _requestContext.UserAgent;
                });

            var stateToken = await _clientAccountRecoveryService.StartRecoveryAsync(startRecoveryDto);

            var response = new RecoveryStartResponseModel
            {
                StateToken = stateToken
            };

            return Ok(response);
        }

        /// <summary>Get current recovery status.</summary>
        [HttpGet]
        [Route("status")]
        [ProducesResponseType(typeof(RecoveryStatusResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecoveryStatus([FromQuery] RecoveryStatusRequestModel model)
        {
            var recoveryStatus = await _clientAccountRecoveryService.GetRecoveryStatusAsync(model.StateToken);

            var response = _mapper.Map<RecoveryStatus, RecoveryStatusResponseModel>(recoveryStatus);

            return Ok(response);
        }

        /// <summary>Submit challenge to continue recovery process.</summary>
        [HttpPost]
        [Route("challenge")]
        [ProducesResponseType(typeof(RecoverySubmitChallengeResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitChallenge([FromBody] RecoverySubmitChallengeRequestModel model)
        {
            var submitChallengeDto = _mapper.Map<RecoverySubmitChallengeRequestModel, RecoverySubmitChallengeDto>(model,
                opt =>
                {
                    opt.Items["Ip"] = _requestContext.GetIp();
                    opt.Items["UserAgent"] = _requestContext.UserAgent;
                });

            var newState =
                await _clientAccountRecoveryService.SubmitChallengeAsync(submitChallengeDto);

            var response = new RecoverySubmitChallengeResponseModel
            {
                StateToken = newState
            };

            return Ok(response);
        }

        /// <summary>Upload selfie file.</summary>
        [HttpPost]
        [Route("file")]
        [ProducesResponseType(typeof(RecoveryUploadFileResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SelfieMaxSize]
        public async Task<IActionResult> UploadFile([FromForm]RecoveryUploadFileRequestModel model)
        {
            var file = model.File;

            var fileId = await _clientAccountRecoveryService.UploadSelfieFileAsync(file);

            var response = new RecoveryUploadFileResponseModel
            {
                FileId = fileId
            };

            return Ok(response);
        }

        /// <summary>Complete recovery by setting new password.</summary>
        [HttpPost]
        [Route("password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Complete([FromBody] RecoveryCompleteRequestModel model)
        {
            var recoveryCompleteDto = _mapper.Map<RecoveryCompleteRequestModel, RecoveryCompleteDto>(model,
                opt =>
                {
                    opt.Items["Ip"] = _requestContext.GetIp();
                    opt.Items["UserAgent"] = _requestContext.UserAgent;
                });

            await _clientAccountRecoveryService.CompleteRecoveryAsync(recoveryCompleteDto);
            return Ok();
        }
    }
}