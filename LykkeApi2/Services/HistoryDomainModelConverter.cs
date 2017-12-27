using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsHistory.Client.Models;
using Lykke.Service.OperationsRepository.Contract;
using Lykke.Service.OperationsRepository.Contract.Cash;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.History;

namespace LykkeApi2.Services
{
    public class HistoryDomainModelConverter
    {
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsCache;
        private readonly IRequestContext _requestContext;
        private readonly ILog _log;

        public HistoryDomainModelConverter(
            CachedDataDictionary<string, Asset> assetsCache,
            CachedDataDictionary<string, AssetPair> assetPairsCache,
            IRequestContext requestContext,
            ILog log)
        {
            _assetsCache = assetsCache ?? throw new ArgumentNullException(nameof(assetsCache));
            _assetPairsCache = assetPairsCache ?? throw new ArgumentNullException(nameof(assetPairsCache));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        }

        public async Task<ApiHistoryOperation> ToApiModel(HistoryRecordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomData)) return null;

            var asset = (await _assetsCache.Values()).FirstOrDefault(x => x.Id == model.Currency);
            if (asset == null)
            {
                await _log.WriteWarningAsync(nameof(HistoryDomainModelConverter), nameof(ToApiModel),
                    $"Unable to find asset in dictionary for assetId = {model.Currency}, walletId = {model.WalletId}");
                return null;
            }

            var legacyOperationType = (OperationType) Enum.Parse(typeof(OperationType), model.OpType);

            ApiCashInHistoryOperation cashIn = null;
            ApiCashOutHistoryOperation cashOut = null;
            ApiTradeHistoryOperation trade = null;

            switch (legacyOperationType)
            {
                case OperationType.CashInOut:
                    var cashInOut = NetJSON.NetJSON.Deserialize<CashOperationDto>(model.CustomData);
                    cashIn = cashInOut.ConvertToCashInApiModel(asset);
                    cashOut = cashInOut.ConvertToCashOutApiModel(asset);
                    break;
                case OperationType.CashOutAttempt:
                    var cashOutRequest = NetJSON.NetJSON.Deserialize<CashOutRequestDto>(model.CustomData);
                    cashOut = cashOutRequest.ConvertToApiModel(asset);
                    break;
                case OperationType.ClientTrade:
                    var clientTrade = NetJSON.NetJSON.Deserialize<ClientTradeDto>(model.CustomData);
                    trade = clientTrade.ConvertToApiModel(asset);
                    break;
                case OperationType.LimitTradeEvent:
                    var limitTrade = NetJSON.NetJSON.Deserialize<LimitTradeEventDto>(model.CustomData);
                    trade = await ConvertLimitTradeEvent(limitTrade, model.WalletId);
                    break;
                case OperationType.TransferEvent:
                    var transfer = NetJSON.NetJSON.Deserialize<TransferEventDto>(model.CustomData);
                    if (_requestContext.ClientId == transfer.ClientId)
                    {
                        cashIn = transfer.ConvertToCashInApiModel(asset);
                    }
                    if (_requestContext.ClientId == transfer.FromId)
                    {
                        cashOut = transfer.ConvertToCashOutApiModel(asset);
                    }
                    break;
                default:
                    throw new Exception($"Unknown operation type: {legacyOperationType.ToString()}");
            }

            var operationId = cashIn?.Id ??
                              cashOut?.Id ??
                              trade?.Id;

            return ApiHistoryOperation.Create(
                id: operationId,
                dateTime: model.DateTime,
                cashIn: cashIn,
                cashout: cashOut,
                trade: trade
            );
        }

        private async Task<ApiTradeHistoryOperation> ConvertLimitTradeEvent(LimitTradeEventDto model, string walletId)
        {
            if (model == null) return null;

            var assetPair = (await _assetPairsCache.Values()).FirstOrDefault(x => x.Id == model.AssetPair);
            if (assetPair == null)
            {
                await _log.WriteWarningAsync(nameof(HistoryDomainModelConverter), nameof(ConvertLimitTradeEvent),
                    $"Unable to find asset pair in dictionary for assetPairId = {model.AssetPair}, walletId = {walletId}");
                return null;
            }

            var asset = (await _assetsCache.Values()).FirstOrDefault(x => x.Id == assetPair.QuotingAssetId);
            if (asset == null)
            {
                await _log.WriteWarningAsync(nameof(HistoryDomainModelConverter), nameof(ConvertLimitTradeEvent),
                    $"Unable to find asset in dictionary for limit trade quoting assetId = {assetPair.QuotingAssetId}, walletId = {walletId}");
                return null;
            }

            return model.ConvertToApiModel(assetPair, asset.GetDisplayAccuracy());
        }
    }
}