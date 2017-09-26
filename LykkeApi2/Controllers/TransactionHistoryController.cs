using AzureRepositories.CashOperations;
using Common;
using Common.Log;
using Core.CashOperations;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Lykke.Service.Wallets.Client;
using LykkeApi2.Mappers;
using LykkeApi2.Models;
using LykkeApi2.Models.ApiContractModels;
using LykkeApi2.Models.TransactionsModels;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CashInOutOperation = Core.CashOperations.CashInOutOperation;
using ClientTrade = Core.CashOperations.ClientTrade;
using TransferEvent = Core.CashOperations.TransferEvent;

namespace LykkeApi2.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    public class TransactionHistoryController : Controller
    {
        private readonly ILog _log;

        private readonly ITradeOperationsRepositoryClient _clientTradesRepositoryClient;
        private readonly ITransferOperationsRepositoryClient _transferEventsRepositoryClient;
        private readonly ICashOperationsRepositoryClient _cashOperationsRepositoryClient;
        private readonly ICashOutAttemptOperationsRepositoryClient _cashOutAttemptRepositoryClient;
        private readonly IWalletsClient _walletsClient;

        private readonly CachedDataDictionary<string, IAssetPair> _assetPairs;
        private readonly CachedDataDictionary<string, IAsset> _assets;

        private readonly ILimitTradeEventsRepository _limitTradeEventsRepository;
        private readonly IMarketOrdersRepository _marketOrdersRepository;

        public TransactionHistoryController(
             ILog log,
             ITradeOperationsRepositoryClient clientTradesRepositoryClient,
             ITransferOperationsRepositoryClient transferEventsRepositoryClient,
             ICashOperationsRepositoryClient cashOperationsRepositoryClient,
             ICashOutAttemptOperationsRepositoryClient cashOutAttemptRepositoryClient,
             IWalletsClient walletsClient,

             CachedDataDictionary<string, IAssetPair> assetPairs,
             CachedDataDictionary<string, IAsset> assets,

             IMarketOrdersRepository marketOrdersRepository,
             ILimitTradeEventsRepository limitTradeEventsRepository)
        {
            _log = log;

            _clientTradesRepositoryClient = clientTradesRepositoryClient;
            _transferEventsRepositoryClient = transferEventsRepositoryClient;
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient;
            _walletsClient = walletsClient;
            _cashOutAttemptRepositoryClient = cashOutAttemptRepositoryClient;

            _assetPairs = assetPairs;
            _assets = assets;

            _marketOrdersRepository = marketOrdersRepository;
            _limitTradeEventsRepository = limitTradeEventsRepository;
        }

        [HttpGet]
        [SwaggerOperation("GetTransactionHistories")]
        public async Task<IActionResult> Get([FromQuery]string assetId)
        {
            //Until we don't have Authorization functionality we could not use the logic for getting automatically client Id for authorized user

            //var clientId = "09ee497c-6dc6-49b4-8afd-45169ab7253b"; //this.GetClientId(); //no wallets client id

            var clientId = "35302a53-cacb-4052-b5c0-57f9c819495b"; //has wallets client id

            var clientTrades = new IClientTrade[0];
            var cashOperations = new ICashInOutOperation[0];
            var transfers = new ITransferEvent[0];
            var cashOutAttempts = new ICashOutRequest[0];
            var limitTradeEvents = new ILimitTradeEvent[0];

            var assets = await _assets.GetDictionaryAsync();
            var assetPairs = await _assetPairs.GetDictionaryAsync();

            var walletsCredentials = (await _walletsClient.GetWalletCredential(clientId));
            var walletsCredentialsHistory = (await _walletsClient.GetWalletCredentialHistory(clientId));

            var clientMultisigs = new List<string>();

            if (walletsCredentials != null && walletsCredentials.MultiSig != null)
            {
                clientMultisigs.Add(walletsCredentials.MultiSig);
            }

            if (walletsCredentialsHistory != null && walletsCredentialsHistory.WalletsCredentialHistory != null)
            {
                clientMultisigs.AddRange(walletsCredentialsHistory.WalletsCredentialHistory);
            }

            /*_walletCredentialsRepository.GetAllClientMultisigs(_walletCredentialsHistoryRepository, clientId);*/

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

                    _cashOutAttemptRepositoryClient.GetRequestsAsync(clientId)
                        .ContinueWith(
                            task =>
                            {
                                var operations = OperationsRepositoryMapper.Instance.Map<IEnumerable<SwiftCashOutRequest>>(
                                    task.Result);
                                cashOutAttempts = operations
                                    .Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden)
                                    .ToArray();
                            }),
                     _limitTradeEventsRepository.GetEventsAsync(clientId)
                        .ContinueWith(
                            task => limitTradeEvents = task.Result.Where(itm => assets.ContainsKey(itm.AssetId) && !itm.IsHidden).ToArray())
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
                                    var operations = OperationsRepositoryMapper.Instance.Map<IEnumerable<CashInOutOperation>>(
                                        task.Result);
                                    cashOperations = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),

                        _transferEventsRepositoryClient.GetByMultisigsAsync(clientMultisigs.ToArray()).
                            ContinueWith(
                                task =>
                                {
                                    var operations =
                                        OperationsRepositoryMapper.Instance
                                            .Map<IEnumerable<TransferEvent>>(task.Result);
                                    transfers = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),

                        _cashOutAttemptRepositoryClient.GetRequestsAsync(clientId)
                            .ContinueWith(
                                task =>
                                {
                                    var operations = OperationsRepositoryMapper.Instance
                                        .Map<IEnumerable<SwiftCashOutRequest>>(task.Result);
                                    cashOutAttempts = operations
                                        .Where(itm => itm.AssetId == assetId && !itm.IsHidden)
                                        .ToArray();
                                }),
                        _limitTradeEventsRepository.GetEventsAsync(clientId)
                            .ContinueWith(
                                task => limitTradeEvents = task.Result.Where(itm => itm.AssetId == assetId && !itm.IsHidden).ToArray())
                        );
            }

            var marketOrders = (await _marketOrdersRepository.GetOrdersAsync(clientTrades.Where(x => x.IsLimitOrderResult).Select(x => x.MarketOrderId).Distinct()))
                .GroupBy(x => x.Id).Select(x => x.First()).ToDictionary(x => x.Id);

            var apiClientTrades = ApiTransactionsConvertor.GetClientTrades(clientTrades, assets, assetPairs, marketOrders);

            return Ok(
                TransactionsResponseModel.Create(
                    apiClientTrades,
                    cashOperations.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    transfers.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    cashOutAttempts.Select(itm => itm.ConvertToApiModel(assets[itm.AssetId])).ToArray(),
                    new ApiCashOutCancelled[0], //not implemented yet, ToDo
                    new ApiCashOutDone[0], //not implemented yet, ToDo
                    limitTradeEvents.Select(itm => itm.ConvertToApiModel(assetPairs[itm.AssetPair], assets[assetPairs[itm.AssetPair].QuotingAssetId].Accuracy)).ToArray()
                ));
        }
    }
}
