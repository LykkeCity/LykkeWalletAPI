using Common;
using Common.Log;
using Core.CashOperations;
using Core.Exchange;
using Core.Mappers;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsHistory.Client.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Lykke.Service.OperationsRepository.Client.Abstractions.Exchange;
using LykkeApi2.Mappers;
using LykkeApi2.Models.ApiContractModels;
using LykkeApi2.Models.TransactionHistoryModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.Client;
using LykkeApi2.Infrastructure;
using CashInOutOperation = Core.CashOperations.CashInOutOperation;
using ClientTrade = Core.CashOperations.ClientTrade;
using TransferEvent = Core.CashOperations.TransferEvent;
using Microsoft.AspNetCore.Authorization;
using Lykke.Service.OperationsRepository.Client.Abstractions.OperationsDetails;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/transactionHistory")]
    public class TransactionHistoryController : Controller
    {
        private readonly ILog _log;

        private readonly ITradeOperationsRepositoryClient _clientTradesRepositoryClient;
        private readonly ITransferOperationsRepositoryClient _transferEventsRepositoryClient;
        private readonly ICashOperationsRepositoryClient _cashOperationsRepositoryClient;
        private readonly ICashOutAttemptOperationsRepositoryClient _cashOutAttemptRepositoryClient;
        private readonly ILimitTradeEventsRepositoryClient _limitTradeEventsRepositoryClient;
        private readonly ILimitOrdersRepositoryClient _limitOrdersRepositoryClient;
        private readonly IMarketOrdersRepositoryClient _marketOrdersRepositoryClient;
        private readonly IBalancesClient _balancesClient;
        private readonly IOperationsHistoryClient _historyClient;
        private readonly IHistoryOperationMapper<object, HistoryOperationSourceData> _historyOperationMapper;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairs;
        private readonly CachedDataDictionary<string, Asset> _assets;
        private readonly IRequestContext _requestContext;

        private readonly IOperationDetailsInformationClient _operationDetailsInformationClient;

        public TransactionHistoryController(
            ILog log,
            ITradeOperationsRepositoryClient clientTradesRepositoryClient,
            ITransferOperationsRepositoryClient transferEventsRepositoryClient,
            ICashOperationsRepositoryClient cashOperationsRepositoryClient,
            ICashOutAttemptOperationsRepositoryClient cashOutAttemptRepositoryClient,
            ILimitTradeEventsRepositoryClient limitTradeEventsRepositoryClient,
            ILimitOrdersRepositoryClient limitOrdersRepositoryClient,
            IMarketOrdersRepositoryClient marketOrdersRepositoryClient,
            IBalancesClient balancesClient,
            IOperationsHistoryClient historyClient,
            IHistoryOperationMapper<object, HistoryOperationSourceData> historyOperationMapper,
            CachedDataDictionary<string, AssetPair> assetPairs,
            CachedDataDictionary<string, Asset> assets,
            IRequestContext requestContext,
            IOperationDetailsInformationClient operationDetailsInformationClient)
        {
            _log = log;
            _clientTradesRepositoryClient = clientTradesRepositoryClient;
            _transferEventsRepositoryClient = transferEventsRepositoryClient;
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient;
            _cashOutAttemptRepositoryClient = cashOutAttemptRepositoryClient;
            _limitTradeEventsRepositoryClient = limitTradeEventsRepositoryClient;
            _limitOrdersRepositoryClient = limitOrdersRepositoryClient;
            _marketOrdersRepositoryClient = marketOrdersRepositoryClient;
            _balancesClient = balancesClient;
            _historyClient = historyClient;
            _historyOperationMapper = historyOperationMapper;
            _assetPairs = assetPairs;
            _assets = assets;
            _requestContext = requestContext;
            _operationDetailsInformationClient = operationDetailsInformationClient;
        }

        /// <summary>
        /// Get transaction history.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation("GetTransactionHistories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(TransactionsResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get([FromQuery] string assetId)
        {
            var clientTrades = new IClientTrade[0];
            var cashOperations = new ICashInOutOperation[0];
            var transfers = new ITransferEvent[0];
            var cashOutAttempts = new ICashOutRequest[0];
            var limitTradeEvents = new ILimitTradeEvent[0];
            var marketOrders = new Dictionary<string, MarketOrder>();

            var assets = await _assets.GetDictionaryAsync();
            var assetPairs = await _assetPairs.GetDictionaryAsync();

            var walletsCredentials = (await _balancesClient.GetWalletCredential(_requestContext.ClientId));
            var walletsCredentialsHistory = (await _balancesClient.GetWalletCredentialHistory(_requestContext.ClientId));

            var clientMultisigs = new List<string>();

            if (walletsCredentials != null && walletsCredentials.MultiSig != null)
            {
                clientMultisigs.Add(walletsCredentials.MultiSig);
            }

            if (walletsCredentialsHistory != null && walletsCredentialsHistory.WalletsCredentialHistory != null)
            {
                clientMultisigs.AddRange(walletsCredentialsHistory.WalletsCredentialHistory);
            }

            if ((string.IsNullOrEmpty(assetId) || assetId.ToLower() == "null") && clientMultisigs.Count > 0)
            {
                await Task.WhenAll(
                    _clientTradesRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                        .ContinueWith(
                            task =>
                            {
                                var operations =
                                    OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(task.Result);
                                clientTrades = operations
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            }),
                    _cashOperationsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                        .ContinueWith(task =>
                        {
                            var operations = OperationsRepositoryMapper.Instance.Map<IEnumerable<CashInOutOperation>>(
                                task.Result);
                            cashOperations = operations
                                .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                .ToArray();
                        }),
                    _transferEventsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                        .ContinueWith(
                            task =>
                            {
                                var operations =
                                    OperationsRepositoryMapper.Instance.Map<IEnumerable<TransferEvent>>(task.Result);
                                transfers = operations
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            }),
                    _cashOutAttemptRepositoryClient.GetRequestsAsync(_requestContext.ClientId)
                        .ContinueWith(
                            task =>
                            {
                                var operations = OperationsRepositoryMapper.Instance
                                    .Map<IEnumerable<SwiftCashOutRequest>>(
                                        task.Result);
                                cashOutAttempts = operations
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            }),
                    _limitTradeEventsRepositoryClient.GetAsync(_requestContext.ClientId).ContinueWith(
                        task =>
                        {
                            var limitTradeEventsResult =
                                OperationsRepositoryMapper.Instance.Map<IEnumerable<LimitTradeEvent>>(
                                    task.Result);
                            limitTradeEvents = limitTradeEventsResult
                                .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                .ToArray();
                        })
                );
            }
            else
            {
                //checking for assetid we check and if there is no wallet credential or wallet history credential jsut skip
                if (assets.ContainsKey(assetId) && clientMultisigs.Count > 0)
                    await Task.WhenAll(
                        _clientTradesRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                            .ContinueWith(
                                task =>
                                {
                                    var operations =
                                        OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(task.Result);
                                    clientTrades = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _cashOperationsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                            .ContinueWith(
                                task =>
                                {
                                    var operations = OperationsRepositoryMapper.Instance
                                        .Map<IEnumerable<CashInOutOperation>>(
                                            task.Result);
                                    cashOperations = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _transferEventsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray()).ContinueWith(
                            task =>
                            {
                                var operations =
                                    OperationsRepositoryMapper.Instance
                                        .Map<IEnumerable<TransferEvent>>(task.Result);
                                transfers = operations
                                    .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                    .ToArray();
                            }),
                        _cashOutAttemptRepositoryClient.GetRequestsAsync(_requestContext.ClientId)
                            .ContinueWith(
                                task =>
                                {
                                    var operations = OperationsRepositoryMapper.Instance
                                        .Map<IEnumerable<SwiftCashOutRequest>>(task.Result);
                                    cashOutAttempts = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _limitTradeEventsRepositoryClient.GetAsync(_requestContext.ClientId).ContinueWith(
                            task =>
                            {
                                var limitTradeEventsResult =
                                    OperationsRepositoryMapper.Instance.Map<IEnumerable<LimitTradeEvent>>(
                                        task.Result);
                                limitTradeEvents = limitTradeEventsResult
                                    .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                    .ToArray();
                            })
                    );
            }

            if (clientTrades.Count() > 0 && !clientTrades.Any(x => string.IsNullOrEmpty(x.MarketOrderId)))
            {
                var marketOrdersResult = OperationsRepositoryMapper.Instance.Map<IEnumerable<MarketOrder>>(
                    _marketOrdersRepositoryClient.GetOrdersAsync(clientTrades.Where(x => !x.IsLimitOrderResult)
                        .Select(x => x.MarketOrderId)
                        .Distinct()
                        .ToArray()).Result);
                if (marketOrdersResult != null && marketOrdersResult.Count() > 0)
                {
                    marketOrders = marketOrdersResult.GroupBy(x => x.Id).Select(x => x.First()).ToDictionary(x => x.Id);
                }
            }

            var apiClientTrades =
                ApiTransactionsConvertor.GetClientTrades(clientTrades, assets, assetPairs, marketOrders);

            return Ok(
                TransactionsResponseModel.Create(
                    apiClientTrades,
                    cashOperations.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    transfers.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    cashOutAttempts.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    new ApiCashOutCancelled[0], //not implemented yet, ToDo
                    new ApiCashOutDone[0], //not implemented yet, ToDo
                    limitTradeEvents.Select(itm => itm.ConvertToApiModel(assetPairs[itm.AssetPair],
                        assets[assetPairs[itm.AssetPair].QuotingAssetId].Accuracy)).ToArray()
                ));
        }

        /// <summary>
        /// Get transaction history with every row containing information about trade, cash-in/cash-out, transfers and limit orders.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>

        [HttpGet("history")]
        [SwaggerOperation("GetHistory")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(List<TransactionHistoryResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetHistory([FromQuery] string assetId = null)
        {
            var clientId = _requestContext.ClientId;

            var assets = await _assets.GetDictionaryAsync();
            var assetPairs = await _assetPairs.GetDictionaryAsync();
            var marketOrders = new Dictionary<string, MarketOrder>();

            string[] availableAssetIds = string.IsNullOrEmpty(assetId) || assetId.ToLower() == "null"
                ? assets.Keys.ToArray()
                : new string[] { assetId };

            IClientTrade[] clientTrades = new IClientTrade[0];
            ICashInOutOperation[] cashOperations = new ICashInOutOperation[0];
            ITransferEvent[] transfers = new ITransferEvent[0];
            ICashOutRequest[] cashOutAttempts = new ICashOutRequest[0];
            ILimitTradeEvent[] limitEvents = new ILimitTradeEvent[0];

            var walletsCredentials = (await _balancesClient.GetWalletCredential(_requestContext.ClientId));
            var walletsCredentialsHistory = (await _balancesClient.GetWalletCredentialHistory(_requestContext.ClientId));

            var clientMultisigs = new List<string>();

            if (walletsCredentials != null && walletsCredentials.MultiSig != null)
            {
                clientMultisigs.Add(walletsCredentials.MultiSig);
            }

            if (walletsCredentialsHistory != null && walletsCredentialsHistory.WalletsCredentialHistory != null)
            {
                clientMultisigs.AddRange(walletsCredentialsHistory.WalletsCredentialHistory);
            }

            if (clientMultisigs.Count() > 0)
            {
                await Task.WhenAll(
                        _clientTradesRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                            .ContinueWith(
                                task =>
                                {
                                    var operations =
                                        OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(task.Result);
                                    clientTrades = operations
                                        .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _cashOperationsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                            .ContinueWith(task =>
                            {
                                var operations = OperationsRepositoryMapper.Instance.Map<IEnumerable<CashInOutOperation>>(
                                    task.Result);
                                cashOperations = operations
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            }),
                        _transferEventsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray())
                            .ContinueWith(
                                task =>
                                {
                                    var operations =
                                        OperationsRepositoryMapper.Instance.Map<IEnumerable<TransferEvent>>(task.Result);
                                    transfers = operations
                                        .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _cashOutAttemptRepositoryClient.GetRequestsAsync(_requestContext.ClientId)
                            .ContinueWith(
                                task =>
                                {
                                    var operations = OperationsRepositoryMapper.Instance
                                        .Map<IEnumerable<SwiftCashOutRequest>>(
                                            task.Result);
                                    cashOutAttempts = operations
                                        .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _limitTradeEventsRepositoryClient.GetAsync(_requestContext.ClientId).ContinueWith(
                            task =>
                            {
                                var limitTradeEventsResult =
                                    OperationsRepositoryMapper.Instance.Map<IEnumerable<LimitTradeEvent>>(
                                        task.Result);
                                limitEvents = limitTradeEventsResult
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            })
                    );
            }

            if (clientTrades.Count() > 0 && !clientTrades.Any(x => string.IsNullOrEmpty(x.MarketOrderId)))
            {
                var marketOrdersResult = OperationsRepositoryMapper.Instance.Map<IEnumerable<MarketOrder>>(
                    _marketOrdersRepositoryClient.GetOrdersAsync(clientTrades.Where(x => !x.IsLimitOrderResult)
                        .Select(x => x.MarketOrderId)
                        .Distinct()
                        .ToArray()).Result);
                if (marketOrdersResult != null && marketOrdersResult.Count() > 0)
                {
                    marketOrders = marketOrdersResult.GroupBy(x => x.Id).Select(x => x.First()).ToDictionary(x => x.Id);
                }
            }

            List<TransactionHistoryResponseModel> result = new List<TransactionHistoryResponseModel>();

            // market trades
            var apiClientTrades =
              ApiTransactionsConvertor.GetClientTrades(clientTrades, assets, assetPairs, marketOrders);

            result.AddRange(
                apiClientTrades.Select(
                    x => TransactionHistoryResponseModel.Create(Convert.ToDateTime(x.DateTime), x.Id, x)));

            // limit trades
            var apiLimitClientTrades =
                ApiTransactionsConvertor.GetLimitClientTrades(clientTrades, assets);
            result.AddRange(
                apiLimitClientTrades.Select(
                    x => TransactionHistoryResponseModel.Create(Convert.ToDateTime(x.DateTime), x.Id, x)));

            result.AddRange(
                limitEvents.Select(
                    x => TransactionHistoryResponseModel.Create(x.CreatedDt, x.Id,
                        limitTradeEvent: x.ConvertToApiModel(assetPairs[x.AssetPair], assets[assetPairs[x.AssetPair].QuotingAssetId].Accuracy))));
            result.AddRange(
                cashOperations.Select(
                    x => TransactionHistoryResponseModel.Create(x.DateTime, x.Id,
                        cashInOut: x.ConvertToApiModel(assets[x.AssetId]))));
            result.AddRange(
                transfers.Select(
                    x => TransactionHistoryResponseModel.Create(x.DateTime, x.Id,
                        transfer: x.ConvertToApiModel(assets[x.AssetId]))));
            result.AddRange(
                cashOutAttempts.Select(
                    x => TransactionHistoryResponseModel.Create(x.DateTime, x.Id,
                        cashOutAttempt: x.ConvertToApiModel(assets[x.AssetId]))));

            var ordered = result.OrderByDescending(x => x.DateTime);

            return Ok(ordered);
        }

        /// <summary>
        /// Limit order and trades.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet("limit/ordersAndTrades")]
        [SwaggerOperation("GetLimitOrderAndTrades")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(ApiLimitOrdersAndTrades), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> LimitOrderAndTrades([FromQuery] string orderId)
        {
            var order = OperationsRepositoryMapper.Instance.Map<LimitOrder>(
                _limitOrdersRepositoryClient.GetOrderAsync(orderId).Result);

            if (order == null)
            {
                return NotFound(Phrases.NoLimitOrder);
            }

            if (order.ClientId != _requestContext.ClientId)
                return BadRequest(Phrases.InvalidValue);

            var assetPair = await _assetPairs.GetItemAsync(order.AssetPairId);

            if (assetPair == null)
            {
                return NotFound(Phrases.AssetPairNotFound);
            }

            var assets = await _assets.GetDictionaryAsync();
            var asset = await _assets.GetItemAsync(assetPair.QuotingAssetId);

            if (asset == null)
            {
                return NotFound(Phrases.AssetNotFound);
            }

            var availableAssetIds = assets.Keys.ToArray();

            var responseClientTrades = (await _clientTradesRepositoryClient.GetByOrderAsync(orderId))
                .Where(itm =>
                    itm.ClientId == _requestContext.ClientId && availableAssetIds.Contains(itm.AssetId) && itm.IsHidden.HasValue &&
                    !itm.IsHidden.Value)
                .ToArray();

            var clientTrades = OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(responseClientTrades)
                .Cast<IClientTrade>().ToArray();

            var apiLimitClientTrades =
                ApiTransactionsConvertor.GetLimitClientTrades(clientTrades, assets);

            return Ok(order.ConvertToApiModel(assetPair, apiLimitClientTrades, asset.Accuracy));
        }

        /// <summary>
        /// Limit trades.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet("limit/trades")]
        [SwaggerOperation("GetLimitTradesHistories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(IEnumerable<TransactionHistoryResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LimitTrades([FromQuery] string orderId)
        {
            var assets = await _assets.GetDictionaryAsync();

            var availableAssetIds = assets.Keys.ToArray();

            var responseClientTrades = (await _clientTradesRepositoryClient.GetByOrderAsync(orderId))
                .Where(itm =>
                    itm.ClientId == _requestContext.ClientId && availableAssetIds.Contains(itm.AssetId) && itm.IsHidden.HasValue &&
                    !itm.IsHidden.Value)
                .ToArray();

            var result = new List<TransactionHistoryResponseModel>();
            var clientTrades = OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(responseClientTrades)
                .Cast<IClientTrade>().ToArray();
            var apiLimitClientTrades =
                ApiTransactionsConvertor.GetLimitClientTrades(clientTrades, assets);
            result.AddRange(
                apiLimitClientTrades.Select(
                    x => TransactionHistoryResponseModel.Create(Convert.ToDateTime(x.DateTime), x.Id, x)));

            var ordered = result.OrderByDescending(x => x.DateTime);

            if (!ordered.Any())
                return NotFound(Phrases.NoLimitTradesHistory);

            return Ok(ordered);
        }

        /// <summary>
        /// Limit history.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet("limit/history")]
        [SwaggerOperation("GetLimitHistory")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(IEnumerable<TransactionHistoryResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> LimitHistory([FromQuery] string orderId)
        {
            var assets = await _assets.GetDictionaryAsync();
            var assetPairs = await _assetPairs.GetDictionaryAsync();

            var availableAssetIds = assets.Keys.ToArray();

            IClientTrade[] clientTrades = new IClientTrade[0];
            ILimitTradeEvent[] limitEvents = new ILimitTradeEvent[0];

            await Task.WhenAll(
                _clientTradesRepositoryClient.GetByOrderAsync(orderId)
                    .ContinueWith(
                        task =>
                        {
                            var operations =
                                OperationsRepositoryMapper.Instance.Map<IEnumerable<ClientTrade>>(task.Result);

                            clientTrades = operations.Where(itm =>
                                itm.ClientId == _requestContext.ClientId && availableAssetIds.Contains(itm.AssetId) &&
                                !itm.IsHidden).ToArray();
                        }),
                _limitTradeEventsRepositoryClient.GetAsync(_requestContext.ClientId, orderId).ContinueWith(
                    task =>
                    {
                        var limitTradeEventsResult =
                            OperationsRepositoryMapper.Instance.Map<IEnumerable<LimitTradeEvent>>(
                                task.Result);
                        limitEvents = limitTradeEventsResult
                            .Where(itm =>
                                itm.ClientId == _requestContext.ClientId && availableAssetIds.Contains(itm.AssetId) && !itm.IsHidden)
                            .ToArray();
                    })
            );

            var result = new List<TransactionHistoryResponseModel>();

            var apiLimitClientTrades =
                ApiTransactionsConvertor.GetLimitClientTrades(clientTrades, assets);

            result.AddRange(
                apiLimitClientTrades.Select(
                    x => TransactionHistoryResponseModel.Create(Convert.ToDateTime(x.DateTime), x.Id, x)));

            result.AddRange(
                limitEvents.Select(
                    x => TransactionHistoryResponseModel.Create(x.CreatedDt, x.Id,
                        limitTradeEvent: x.ConvertToApiModel(assetPairs[x.AssetPair],
                            assets[assetPairs[x.AssetPair].QuotingAssetId].Accuracy))));

            var ordered = result.OrderByDescending(x => x.DateTime);

            if (!ordered.Any())
                return NotFound(Phrases.NoLimitsHistory);

            return Ok(ordered);
        }


        /// <summary>
        /// Get operation details information for the particular transaction id and for the client who has made the transaction.
        /// </summary>
        [HttpGet("operationsDetail/history")]
        [SwaggerOperation("GeOperationsDetailHistory")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(Lykke.Service.OperationsRepository.AutorestClient.Models.OperationDetailsInformation), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> OperationsDetailHistory([FromQuery] string clientId, [FromQuery] string transactionId)
        {
            var operationDetailsForClient = _operationDetailsInformationClient.GetAsync(clientId).Result;

            if (operationDetailsForClient != null && operationDetailsForClient.Count() > 0)
            {
                var result = operationDetailsForClient.FirstOrDefault(o => o.TransactionId == transactionId);
                if (result != null)
                    return Ok(result);

                return NotFound(Phrases.OperationsDetailInfoNotFound);
            }
            else
            {
                return NotFound(Phrases.OperationsDetailInfoNotFound);
            }
        }

        /// <summary>
        /// Limit order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet("limit/order")]
        [SwaggerOperation("GetLimitOrderHistories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(ApiLimitOrder), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> LimitOrder([FromQuery] string orderId)
        {
            var order = OperationsRepositoryMapper.Instance.Map<LimitOrder>(
                _limitOrdersRepositoryClient.GetOrderAsync(orderId).Result);

            if (order == null)
            {
                return NotFound(Phrases.NoLimitOrder);
            }

            if (order.ClientId != _requestContext.ClientId)
                return BadRequest(Phrases.InvalidValue);

            var assetPair = await _assetPairs.GetItemAsync(order.AssetPairId);

            if (assetPair == null)
            {
                return NotFound(Phrases.AssetPairNotFound);
            }

            var asset = await _assets.GetItemAsync(assetPair.QuotingAssetId);

            if (asset == null)
            {
                return NotFound(Phrases.AssetNotFound);
            }

            return Ok(order.ConvertToApiModel(assetPair, asset.Accuracy));
        }

        /// <summary>
        /// Get client transaction history.
        /// </summary>
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <param name="operationType"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("client")]
        [ApiExplorerSettings(GroupName = "Client")]
        [ProducesResponseType(typeof(IEnumerable<TransactionHistoryResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get([FromQuery] int top = 1000, [FromQuery] int skip = 0,
            [FromQuery] string operationType = null, [FromQuery] string assetId = null)
        {
            var response = await RouteToServiceMethod(top, skip, operationType, assetId);
            if (response.Error != null)
            {
                var firstMessage = response.Error.Messages.First();
                var messageText = string.Concat(firstMessage.Value);
                return NotFound(messageText);
            }

            var mappedResult = response.Records
                .Select(r =>
                {
                    var source = new HistoryOperationSourceData { OperationType = r.OpType, JsonData = r.CustomData };
                    var mapped = _historyOperationMapper.Map(source);
                    return ConvertServiceObjectToHistoryRecord(mapped);
                });
            var orderedResult = mappedResult.OrderByDescending(i => i.DateTime);

            if (!orderedResult.Any())
                return NotFound(Phrases.NoLimitsHistory);

            return Ok(orderedResult);
        }

        #region HelperMethods

        private async Task<OperationsHistoryResponse> RouteToServiceMethod(int top, int skip, string operationType,
            string assetId)
        {
            if (operationType == null)
            {
                if (assetId == null)
                {
                    return await _historyClient.AllAsync(_requestContext.ClientId, top, skip);
                }
                return await _historyClient.ByAssetAsync(_requestContext.ClientId, assetId, top, skip);
            }
            if (assetId == null)
            {
                return await _historyClient.ByOperationAsync(_requestContext.ClientId, operationType, top, skip);
            }
            return await _historyClient.ByOperationAndAssetAsync(_requestContext.ClientId, operationType, assetId, top, skip);
        }

        private static TransactionHistoryResponseModel ConvertServiceObjectToHistoryRecord(object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ApiBalanceChangeModel)
            {
                var op = source as ApiBalanceChangeModel;
                return new TransactionHistoryResponseModel
                {
                    Id = op.Id,
                    DateTime = Convert.ToDateTime(op.DateTime),
                    CashInOut = op
                };
            }
            if (source is ApiCashOutAttempt)
            {
                var op = source as ApiCashOutAttempt;
                return new TransactionHistoryResponseModel
                {
                    Id = op.Id,
                    DateTime = Convert.ToDateTime(op.DateTime),
                    CashOutAttempt = op
                };
            }
            if (source is ApiTransfer)
            {
                var op = source as ApiTransfer;
                return new TransactionHistoryResponseModel
                {
                    Id = op.Id,
                    DateTime = Convert.ToDateTime(op.DateTime),
                    Transfer = op
                };
            }
            if (source is ApiTradeOperation)
            {
                var op = source as ApiTradeOperation;
                return new TransactionHistoryResponseModel
                {
                    Id = op.Id,
                    DateTime = Convert.ToDateTime(op.DateTime),
                    Trade = op
                };
            }

            throw new Exception("Unknown object type");
        }

        #endregion
    }
}