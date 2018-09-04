using System;
using System.Linq;
using System.Net;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Repositories;
using Lykke.Cqrs;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.History.Client;
using Lykke.Service.History.Contracts.Enums;
using Swashbuckle.AspNetCore.SwaggerGen;
using LykkeApi2.Models.History;
using ErrorResponse = LykkeApi2.Models.ErrorResponse;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountService;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IHistoryExportsRepository _historyExportsRepository;
        private readonly IHistoryClient _historyClient;

        public HistoryController(
            IRequestContext requestContext,
            IClientAccountClient clientAccountService,
            ICqrsEngine cqrsEngine,
            IHistoryExportsRepository historyExportsRepository, 
            IHistoryClient historyClient)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _cqrsEngine = cqrsEngine;
            _historyExportsRepository = historyExportsRepository;
            _historyClient = historyClient;
        }

        [HttpPost("client/csv")]
        [SwaggerOperation("RequestClientHistoryCsv")]
        [ProducesResponseType(typeof(RequestClientHistoryCsvResponseModel), (int)HttpStatusCode.OK)]
        public IActionResult RequestClientHistoryCsv([FromBody]RequestClientHistoryCsvRequestModel model)
        {
            var id = Guid.NewGuid().ToString();

            _cqrsEngine.SendCommand(new ExportClientHistoryCommand
            {
                Id = id,
                ClientId = _requestContext.ClientId,
                OperationTypes = model.OperationType,
                AssetId = model.AssetId,
                AssetPairId = model.AssetPairId
            }, null, HistoryExportBuilderBoundedContext.Name);

            return Ok(new RequestClientHistoryCsvResponseModel { Id = id });
        }

        [HttpGet("client/csv")]
        [SwaggerOperation("GetClientHistoryCsv")]
        [ProducesResponseType(typeof(GetClientHistoryCsvResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClientHistoryCsv([FromQuery]string id)
        {
            return Ok(new GetClientHistoryCsvResponseModel { Url = await _historyExportsRepository.GetUrl(_requestContext.ClientId, id) });
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
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<HistoryResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByWalletId(
            string walletId,
            [FromQuery(Name = "operationType")] HistoryType[] operationType,
            [FromQuery] string assetId,
            [FromQuery] string assetPairId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallets = (await _clientAccountService.GetWalletsByClientIdAsync(clientId)).ToList();

            var isTradingWallet = wallets.FirstOrDefault(x => x.Id == walletId)?.Type == "Trading";

            if (!isTradingWallet && !wallets.Any(x => x.Id.Equals(walletId)))
            {
                return NotFound();
            }

            // TODO: remove after migration to wallet id
            if (isTradingWallet)
                walletId = clientId;

            var data = await _historyClient.HistoryApi.GetHistoryByWalletAsync(Guid.Parse(walletId), operationType,
                assetId, assetPairId, offset: skip, limit: take);

            return Ok(data.SelectMany(x => x.ToResponseModel()));
        }
    }
}
