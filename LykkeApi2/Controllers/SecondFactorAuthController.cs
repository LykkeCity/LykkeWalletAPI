using System;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Core.Exceptions;
using Lykke.Cqrs;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.ConfirmationCodes.Client.Models.Request;
using Lykke.Service.ConfirmationCodes.Contract;
using Lykke.Service.ConfirmationCodes.Contract.Commands;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models._2Fa;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Refit;

namespace LykkeApi2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/2fa")]
    public class SecondFactorAuthController : Controller
    {
        private readonly IConfirmationCodesClient _confirmationCodesClient;
        private readonly IRequestContext _requestContext;
        private readonly ICqrsEngine _cqrsEngine;

        public SecondFactorAuthController(
            IConfirmationCodesClient confirmationCodesClient,
            IRequestContext requestContext,
            ICqrsEngine cqrsEngine)
        {
            _confirmationCodesClient = confirmationCodesClient;
            _requestContext = requestContext;
            _cqrsEngine = cqrsEngine;
        }

        /// <summary>
        /// confirm operation with 2fa 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("operation")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public IActionResult ConfirmOperation([FromBody] OperationConfirmationModel model)
        {
            var command = new ConfirmCommand
            {
                OperationId = model.OperationId,
                ClientId = Guid.Parse(_requestContext.ClientId),
                Confirmation = model.Signature.Code
            };

            _cqrsEngine.SendCommand(command, "apiv2", OperationsBoundedContext.Name);

            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailable()
        {
            return await _confirmationCodesClient.Google2FaClientHasSetupAsync(_requestContext.ClientId)
                ? Ok(new string[] { "google" })
                : Ok(new string[] { });
        }

        [HttpGet("setup/google")]
        [ProducesResponseType(typeof(GoogleSetupRequestResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetupGoogle2FaRequest()
        {
            try
            {
                var resp = await _confirmationCodesClient.Google2FaRequestSetupAsync(
                    new RequestSetupGoogle2FaRequest { ClientId = _requestContext.ClientId });

                return Ok(new GoogleSetupRequestResponse { ManualEntryKey = resp.ManualEntryKey });
            }
            catch (ApiException e)
            {
                if (e.StatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InconsistentState);

                throw;
            }
        }

        [HttpPost("setup/google")]
        [ProducesResponseType(typeof(GoogleSetupVerifyResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetupGoogle2FaVerify([FromBody] GoogleSetupVerifyRequest model)
        {
            try
            {
                var resp = await _confirmationCodesClient.Google2FaVerifySetupAsync(
                    new VerifySetupGoogle2FaRequest { ClientId = _requestContext.ClientId, Code = model.Code });

                return Ok(new GoogleSetupVerifyResponse { IsValid = resp.IsValid });
            }
            catch (ApiException e)
            {
                if (e.StatusCode == HttpStatusCode.BadRequest)
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InconsistentState);

                throw;
            }
        }
    }
}