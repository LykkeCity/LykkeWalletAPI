using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsHistory.Client.Models;
using Lykke.Service.OperationsRepository.Contract;
using Lykke.Service.OperationsRepository.Contract.Cash;
using LykkeApi2.Models;
using LykkeApi2.Models.History;
using LykkeApi2.Models.Operations;
using Newtonsoft.Json;

namespace LykkeApi2.Services
{
    public class DomainModelConverter
    {
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsCache;
        private readonly ILog _log;

        public DomainModelConverter(
            CachedDataDictionary<string, Asset> assetsCache,
            CachedDataDictionary<string, AssetPair> assetPairsCache,
            ILog log)
        {
            _assetsCache = assetsCache ?? throw new ArgumentNullException(nameof(assetsCache));
            _assetPairsCache = assetPairsCache ?? throw new ArgumentNullException(nameof(assetPairsCache));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<ApiHistoryOperation> ToApiModel(HistoryRecordModel model)
        {
            var operationType = (OperationType) Enum.Parse(typeof(OperationType), model.OpType);

            CashOperationDto cashInOut = null;
            CashOutRequestDto cashoutAttempt = null;
            ClientTradeDto clientTrade = null;
            LimitTradeEventDto limitTradeEvent = null;
            TransferEventDto transferEvent = null;

            var json = model.CustomData;
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            switch (operationType)
            {
                case OperationType.CashInOut:
                    cashInOut = await Deserialize<CashOperationDto>(json);
                    break;
                case OperationType.CashOutAttempt:
                    cashoutAttempt = await Deserialize<CashOutRequestDto>(json);
                    break;
                case OperationType.ClientTrade:
                    clientTrade = await Deserialize<ClientTradeDto>(json);
                    break;
                case OperationType.LimitTradeEvent:
                    limitTradeEvent = await Deserialize<LimitTradeEventDto>(json);
                    break;
                case OperationType.TransferEvent:
                    transferEvent = await Deserialize<TransferEventDto>(json);
                    break;
                default:
                    throw new Exception($"Unknown operation type: {operationType.ToString()}");
            }

            var operationId = cashInOut?.Id ??
                              cashoutAttempt?.Id ??
                              clientTrade?.Id ??
                              limitTradeEvent?.Id ??
                              transferEvent?.Id;

            var asset = (await _assetsCache.Values()).FirstOrDefault(x => x.Id == model.Currency);
            if (asset == null)
            {
                await _log.WriteWarningAsync(nameof(DomainModelConverter), nameof(ToApiModel),
                    $"Unable to find asset in dictionary for assetId = {model.Currency}, walletId = {model.WalletId}");
                return null;
            }

            return ApiHistoryOperation.Create(
                id: operationId,
                dateTime: model.DateTime,
                trade: clientTrade?.ConvertToApiModel(asset),
                cashInOut: cashInOut?.ConvertToApiModel(asset),
                cashOutAttempt: cashoutAttempt?.ConvertToApiModel(asset),
                transfer: transferEvent?.ConvertToApiModel(asset),
                limitTradeEvent: await ConvertLimitTradeEvent(limitTradeEvent, model.WalletId));
        }

        private async Task<ApiLimitTradeEvent> ConvertLimitTradeEvent(LimitTradeEventDto model, string walletId)
        {
            if (model == null) return null;

            var assetPair = (await _assetPairsCache.Values()).FirstOrDefault(x => x.Id == model.AssetPair);
            if (assetPair == null)
            {
                await _log.WriteWarningAsync(nameof(DomainModelConverter), nameof(ConvertLimitTradeEvent),
                    $"Unable to find asset pair in dictionary for assetPairId = {model.AssetPair}, walletId = {walletId}");
                return null;
            }

            var asset = (await _assetsCache.Values()).FirstOrDefault(x => x.Id == assetPair.QuotingAssetId);
            if (asset == null)
            {
                await _log.WriteWarningAsync(nameof(DomainModelConverter), nameof(ConvertLimitTradeEvent),
                    $"Unable to find asset in dictionary for limit trade quoting assetId = {assetPair.QuotingAssetId}, walletId = {walletId}");
                return null;
            }

            return model.ConvertToApiModel(assetPair, asset.GetDisplayAccuracy());
        }

        private async Task<T> Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DomainModelConverter), nameof(Deserialize), json, ex);
                throw;
            }
        }
    }
}