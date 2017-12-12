using System;
using System.Linq;
using System.Net;
using Lykke.Service.OperationsHistory.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.SwaggerGen.Annotations;
using LykkeApi2.Models.History;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ValidateModel]
    public class HistoryController : Controller
    {
        private readonly IOperationsHistoryClient _operationsHistoryClient;
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountService;

        public HistoryController(IOperationsHistoryClient operationsHistoryClient, IRequestContext requestContext, IClientAccountClient clientAccountService)
        {
            _operationsHistoryClient = operationsHistoryClient ?? throw new ArgumentNullException(nameof(operationsHistoryClient));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
        }

        [HttpGet("client")]
        [SwaggerOperation("GetByClientId")]
        [ApiExplorerSettings(GroupName = "History")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(IEnumerable<ApiHistoryRecordModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetByClientId(
            [FromQuery] string operationType,
            [FromQuery] string assetId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var response = await _operationsHistoryClient.GetByClientId(clientId, operationType, assetId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            return Ok(response.Records.Select(x => x.ToApiModel()));
        }

        [HttpGet("wallet/{walletId}")]
        [SwaggerOperation("GetByWalletId")]
        [ApiExplorerSettings(GroupName = "History")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IEnumerable<ApiHistoryRecordModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWalletId(
            string walletId,
            [FromQuery] string operationType,
            [FromQuery] string assetId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(clientId);

            if (!wallets.Any(x => x.Id.Equals(wallets)))
            {
                return BadRequest(ErrorResponse.Create("Wallet doesn't exist"));
            }

            var response = await _operationsHistoryClient.GetByWalletId(walletId, operationType, assetId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            return Ok(response.Records.Select(x => x.ToApiModel()));
        }
    }
}
