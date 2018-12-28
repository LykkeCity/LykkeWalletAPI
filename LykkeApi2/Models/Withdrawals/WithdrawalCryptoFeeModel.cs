using System;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.Withdrawals
{
    public class WithdrawalCryptoFeeModel
    {
        public decimal Size { set; get; }
        public WithdrawalFeeType Type { set; get; }
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum WithdrawalFeeType
    {
        Absolute,
        Relative
    }

    public static class CashoutFeeHelper
    {
        public static WithdrawalCryptoFeeModel ToApiModel(this CashoutFee fee)
        {
            if (fee == null)
                return null;
            
            if(fee.Type == FeeType.Unknown)
                throw new Exception($"{fee.AssetId} has fee type set to {fee.Type}");

            return new WithdrawalCryptoFeeModel
            {
                Size = (decimal) fee.Size,
                Type = fee.Type == FeeType.Absolute ? WithdrawalFeeType.Absolute : WithdrawalFeeType.Relative
            };
        }
    }
}