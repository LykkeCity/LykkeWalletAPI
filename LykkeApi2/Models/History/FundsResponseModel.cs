using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.History.Contracts.Enums;
using Lykke.Service.History.Contracts.History;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.History
{
    public enum FundsOperation
    {
        Deposit,
        Withdraw
    }

    public enum FundsType
    {
        Undefined,
        Card,
        Bank,
        Blockchain
    }

    public enum FundsStatus
    {
        Completed
    }

    public class FundsResponseModel
    {
        public Guid Id { get; set; }

        public string AssetId { get; set; }

        public string AssetName { get; set; }

        public decimal Volume { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FundsOperation Operation { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FundsType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FundsStatus Status { get; set; }

        public DateTime Timestamp { get; set; }

        public string BlockchainHash { get; set; }
    }

    public static class FundsModelExtensions
    {
        /// <summary>
        /// Converts history models to API response
        /// </summary>
        /// <param name="historyModel">Should be only of type CashinModel or CashoutModel</param>
        /// <param name="assetsHelper"></param>
        /// <returns></returns>
        public static async Task<FundsResponseModel> ToFundsResponseModel(this BaseHistoryModel historyModel, IAssetsHelper assetsHelper)
        {
            switch (historyModel)
            {
                case CashinModel cashin:
                    {
                        var asset = await assetsHelper.GetAssetAsync(cashin.AssetId);
                        return new FundsResponseModel
                        {
                            Id = cashin.Id,
                            Volume = Math.Abs(cashin.Volume),
                            AssetId = cashin.AssetId,
                            AssetName = asset?.DisplayId ?? cashin.AssetId,
                            Operation = FundsOperation.Deposit,
                            Status = FundsStatus.Completed,
                            Type = FundsType.Undefined,
                            Timestamp = cashin.Timestamp,
                            BlockchainHash = cashin.BlockchainHash
                        };
                    }
                case CashoutModel cashout:
                    {
                        var asset = await assetsHelper.GetAssetAsync(cashout.AssetId);
                        return new FundsResponseModel
                        {
                            Id = cashout.Id,
                            Volume = Math.Abs(cashout.Volume),
                            AssetId = cashout.AssetId,
                            AssetName = asset?.DisplayId ?? cashout.AssetId,
                            Operation = FundsOperation.Withdraw,
                            Status = FundsStatus.Completed,
                            Type = FundsType.Undefined,
                            Timestamp = cashout.Timestamp,
                            BlockchainHash = cashout.BlockchainHash
                        };
                    }
                default:
                    return null;
            }
        }

        public static HistoryType ToHistoryType(this FundsOperation operation)
        {
            return operation == FundsOperation.Deposit ? HistoryType.CashIn : HistoryType.CashOut;
        }
    }
}
