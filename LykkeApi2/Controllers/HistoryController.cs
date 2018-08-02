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
using Lykke.Cqrs;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Service.ClientAccount.Client;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.OperationsHistory.AutorestClient.Models;
using LykkeApi2.Models.History;
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
        private readonly ICqrsEngine _cqrsEngine;

        public HistoryController(
            IOperationsHistoryClient operationsHistoryClient, 
            IRequestContext requestContext, 
            IClientAccountClient clientAccountService,
            ICqrsEngine cqrsEngine)
        {
            _operationsHistoryClient = operationsHistoryClient ?? throw new ArgumentNullException(nameof(operationsHistoryClient));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _cqrsEngine = cqrsEngine;
        }

        /// <summary>
        /// Get history by client identifier
        /// </summary>
        /// <param name="operationType">The types of the operation, possible values: CashIn, CashOut, Trade, LimitTrade, LimitTradeEvent</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="assetPairId">Asset pair identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("client")]
        [SwaggerOperation("GetByClientId")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(IEnumerable<HistoryResponseModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetByClientId(
            [FromQuery] HistoryOperationType[] operationType,
            [FromQuery] string assetId,
            [FromQuery] string assetPairId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var response = await _operationsHistoryClient.GetByClientId(clientId, operationType, assetId, assetPairId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            return Ok(response.Records.Where(x => x != null).Select(x => x.ToResponseModel()));
        }

        [HttpPost("client/csv")]
        [SwaggerOperation("RequestClientHistoryCsv")]
        [ProducesResponseType(typeof(RequestClientHistoryCsvResponseModel), (int)HttpStatusCode.OK)]
        public IActionResult RequestClientHistoryCsv(
            [FromQuery] HistoryOperationType[] operationType,
            [FromQuery] string assetId,
            [FromQuery] string assetPairId,
            [FromQuery] int? take,
            [FromQuery] int skip)
        {
            var id = Guid.NewGuid().ToString();
            
            _cqrsEngine.SendCommand(new ExportClientHistoryCommand
            {
                Id = id,
                ClientId = _requestContext.ClientId,
                OperationTypes = operationType,
                AssetId = assetId,
                AssetPairId = assetPairId,
                Skip = skip,
                Take = take
            }, null, HistoryExportBuilderBoundedContext.Name);

            return Ok(new RequestClientHistoryCsvResponseModel {Id = id});
        }

        [HttpGet("client/csv")]
        [SwaggerOperation("GetClientHistoryCsv")]
        [ProducesResponseType(typeof(GetClientHistoryCsvResponseModel), (int) HttpStatusCode.OK)]
        public IActionResult GetClientHistoryCsv([FromQuery]string id)
        {
            return Ok(new GetClientHistoryCsvResponseModel { });
        }

        /// <summary>
        /// Getting history by wallet identifier
        /// </summary>
        /// <param name="walletId">Wallet identifier</param>
        /// <param name="operationType">The type of the operation, possible values: CashIn, CashOut, Trade, LimitTrade, LimitTradeEvent</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="assetPairId">Asset pair identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("wallet/{walletId}")]
        [SwaggerOperation("GetByWalletId")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<HistoryResponseModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWalletId(
            string walletId,
            [FromQuery] HistoryOperationType[] operationType,
            [FromQuery] string assetId,
            [FromQuery] string assetPairId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(clientId);

            if (!wallets.Any(x => x.Id.Equals(walletId)))
            {
                return NotFound();
            }

            var response = await _operationsHistoryClient.GetByWalletId(walletId, operationType, assetId, assetPairId, take, skip);

            if (response.Error != null)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, response.Error);
            }

            return Ok(response.Records.Where(x => x != null).Select(x => x.ToResponseModel()));
        }
    }
}
