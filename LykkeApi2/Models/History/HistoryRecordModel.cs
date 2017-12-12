using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace LykkeApi2.Models.History
{
    public class ApiHistoryRecordModel
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public ApiBalanceChangeModel CashInOut { get; set; }
        public ApiTradeOperation Trade { get; set; }
        public ApiTransfer Transfer { get; set; }
        public ApiCashOutAttempt CashOutAttempt { get; set; }
        public ApiCashOutCancelled CashOutCancelled { get; set; }
        public ApiCashOutDone CashOutDone { get; set; }
        public ApiLimitTradeEvent LimitTradeEvent { get; set; }

        public static ApiHistoryRecordModel Create(DateTime dateTime, string id, ApiTradeOperation tradeOperation = null,
            ApiBalanceChangeModel cashInOut = null, ApiTransfer transfer = null, ApiCashOutAttempt cashOutAttempt = null,
            ApiCashOutCancelled cashOutCancelled = null, ApiCashOutDone apiCashOutDone = null, ApiLimitTradeEvent limitTradeEvent = null)
        {
            return new ApiHistoryRecordModel
            {
                Id = id,
                DateTime = dateTime,
                CashInOut = cashInOut,
                Trade = tradeOperation,
                Transfer = transfer,
                CashOutAttempt = cashOutAttempt,
                CashOutCancelled = cashOutCancelled,
                CashOutDone = apiCashOutDone,
                LimitTradeEvent = limitTradeEvent
            };
        }
    }

    public class ApiTradeOperation
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
        public string BlockChainHash { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsSettled { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStates State { get; set; }

        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
    }

    public class ApiTransfer
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
        public string BlockChainHash { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsSettled { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStates State { get; set; }
    }


    public class ApiBalanceChangeModel
    {
        public string Id { get; set; }
        public double Amount { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public string IconId { get; set; }
        public string BlockChainHash { get; set; }
        public bool IsRefund { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsSettled { get; set; }
        public string Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStates State { get; set; }
    }

    public class ApiCashOutAttempt
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
    }

    public class ApiLimitTradeEvent
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public DateTime DateTime { get; set; }
        public string Asset { get; set; }
        public string AssetPair { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public double TotalCost { get; set; }
    }

    public class ApiCashOutCancelled
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
    }

    public class ApiCashOutDone
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
    }
}