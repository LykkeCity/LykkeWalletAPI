using System;
using System.Linq;
using System.Net;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Core.Repositories;
using Core.Services;
using Lykke.Cqrs;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.History.Client;
using Lykke.Service.History.Contracts.Enums;
using Lykke.Service.History.Contracts.History;
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
        private readonly IAssetsHelper _assetsHelper;

        public HistoryController(
            IRequestContext requestContext,
            IClientAccountClient clientAccountService,
            ICqrsEngine cqrsEngine,
            IHistoryExportsRepository historyExportsRepository,
            IHistoryClient historyClient,
            IAssetsHelper assetsHelper)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _cqrsEngine = cqrsEngine;
            _historyExportsRepository = historyExportsRepository;
            _historyClient = historyClient;
            _assetsHelper = assetsHelper;
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
        /// <param name="operationType">The type of the operation, possible values: CashIn, CashOut, Trade, OrderEvent</param>
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
            [FromQuery(Name = "operationType")] string[] operationType,
            [FromQuery] string assetId,
            [FromQuery] string assetPairId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            // TODO: should be removed after release. operationType parameter should be of type HistoryType[]
            var types = new HashSet<HistoryType>();
            foreach (var opType in operationType)
            {
                if (Enum.TryParse<HistoryType>(opType, out var result))
                    types.Add(result);
            }

            var wallet = await _clientAccountService.GetWalletAsync(walletId);

            if (wallet == null || wallet.ClientId != clientId)
                return NotFound();

            // TODO: remove after migration to wallet id
            if (wallet.Type == "Trading")
                walletId = clientId;

            var data = await _historyClient.HistoryApi.GetHistoryByWalletAsync(Guid.Parse(walletId), types.ToArray(),
                assetId, assetPairId, offset: skip, limit: take);

            return Ok(data.SelectMany(x => x.ToResponseModel()));
        }

        /// <summary>
        /// Getting history by wallet identifier
        /// </summary>
        /// <param name="walletId">Wallet identifier</param>
        /// <param name="assetPairId">Asset pair identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("{walletId}/trades")]
        [SwaggerOperation("GetTradesByWalletId")]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<TradeResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTradesByWalletId(
            string walletId,
            [FromQuery] string assetPairId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallet = await _clientAccountService.GetWalletAsync(walletId);

            if (wallet == null || wallet.ClientId != clientId)
                return NotFound();

            // TODO: remove after migration to wallet id
            if (wallet.Type == "Trading")
                walletId = clientId;

            var data = await _historyClient.HistoryApi.GetHistoryByWalletAsync(Guid.Parse(walletId), new[] { HistoryType.Trade },
                assetPairId: assetPairId, offset: skip, limit: take);

            var result = await data.SelectAsync(x => x.ToTradeResponseModel(_assetsHelper));

            return Ok(result.OrderByDescending(x => x.Timestamp));
        }

        /// <summary>
        /// Getting history by wallet identifier
        /// </summary>
        /// <param name="walletId">Wallet identifier</param>
        /// <param name="operation"></param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="take">How many maximum items have to be returned</param>
        /// <param name="skip">How many items skip before returning</param>
        /// <returns></returns>
        [HttpGet("{walletId}/funds")]
        [SwaggerOperation("GetFundsByWalletId")]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<FundsResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFundsByWalletId(
            string walletId,
            [FromQuery(Name = "operation")] FundsOperation[] operation,
            [FromQuery] string assetId,
            [FromQuery] int take,
            [FromQuery] int skip)
        {
            var clientId = _requestContext.ClientId;

            var wallet = await _clientAccountService.GetWalletAsync(walletId);

            if (wallet == null || wallet.ClientId != clientId)
                return NotFound();

            // TODO: remove after migration to wallet id
            if (wallet.Type == "Trading")
                walletId = clientId;

            if (operation.Length == 0)
                operation = Enum.GetValues(typeof(FundsOperation)).Cast<FundsOperation>().ToArray();

            var data = await _historyClient.HistoryApi.GetHistoryByWalletAsync(Guid.Parse(walletId), operation.Select(x => x.ToHistoryType()).ToArray(),
                assetId: assetId, offset: skip, limit: take);

            var result = (await data.SelectAsync(x => x.ToFundsResponseModel(_assetsHelper))).ToList();

            foreach (var item in result)
            {
                item.Type = FundsType.Transfer;
            }

            return Ok(result.OrderByDescending(x => x.Timestamp));
        }
    }
}
