using System;
using System.Linq;
using System.Net;
using Lykke.Service.OperationsHistory.Client;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.OperationsHistory.AutorestClient.Models;
using ErrorResponse = LykkeApi2.Models.ErrorResponse;

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

        public HistoryController(
            IOperationsHistoryClient operationsHistoryClient, 
            IRequestContext requestContext, 
            IClientAccountClient clientAccountService)
        {
            _operationsHistoryClient = operationsHistoryClient ?? throw new ArgumentNullException(nameof(operationsHistoryClient));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
        }

        /// <summary>
        /// Get history by client identifier
        /// </summary>
        /// <param name="operationType">The type of the operation, possible values: CashIn, CashOut, Trade</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("client")]
        [SwaggerOperation("GetByClientId")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(IEnumerable<HistoryOperation>), (int) HttpStatusCode.OK)]
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

            return Ok(response.Records.Where(x => x != null));
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
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<HistoryOperation>), (int) HttpStatusCode.OK)]
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
                return NotFound();
            }

            var response = await _operationsHistoryClient.GetByWalletId(walletId, operationType, assetId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            return Ok(response.Records.Where(x => x != null));
        }
    }
}
