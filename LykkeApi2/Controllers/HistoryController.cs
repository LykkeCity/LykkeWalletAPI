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
using LykkeApi2.Services;

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
        private readonly DomainModelConverter _converter;

        public HistoryController(
            IOperationsHistoryClient operationsHistoryClient, 
            IRequestContext requestContext, 
            IClientAccountClient clientAccountService, 
            DomainModelConverter converter)
        {
            _operationsHistoryClient = operationsHistoryClient ?? throw new ArgumentNullException(nameof(operationsHistoryClient));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        /// <summary>
        /// Get history by client identifier
        /// </summary>
        /// <param name="operationType">The type of the operation, possible values: CashInOut, CashOutAttempt, ClientTrade, TransferEvent, LimitTradeEvent</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("client")]
        [SwaggerOperation("GetByClientId")]
        [ApiExplorerSettings(GroupName = "History")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(IEnumerable<ApiHistoryOperation>), (int) HttpStatusCode.OK)]
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

            var convertTasks = response.Records.Select(x => _converter.ToApiModel(x));

            return Ok((await Task.WhenAll(convertTasks)).Where(x => x != null));
        }

        /// <summary>
        /// Getting history by wallet identifier
        /// </summary>
        /// <param name="walletId">Wallet identifier</param>
        /// <param name="operationType">The type of the operation, possible values: CashInOut, CashOutAttempt, ClientTrade, TransferEvent, LimitTradeEvent</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("wallet/{walletId}")]
        [SwaggerOperation("GetByWalletId")]
        [ApiExplorerSettings(GroupName = "History")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(IEnumerable<ApiHistoryOperation>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWalletId(
            string walletId,
            [FromQuery] string operationType,
            [FromQuery] string assetId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(clientId);

            if (!wallets.Any(x => x.Id.Equals(walletId)))
            {
                return BadRequest(ErrorResponse.Create("Wallet doesn't exist"));
            }

            var response = await _operationsHistoryClient.GetByWalletId(walletId, operationType, assetId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            var convertTasks = response.Records.Select(x => _converter.ToApiModel(x));

            return Ok((await Task.WhenAll(convertTasks)).Where(x => x != null));
        }
    }
}
