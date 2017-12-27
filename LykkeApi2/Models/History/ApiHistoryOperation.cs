using Lykke.Service.OperationsRepository.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace LykkeApi2.Models.History
{
    public enum ApiCashOutState
    {
        Regular = 0,
        Request,
        Done,
        Cancelled
    }

    public enum ApiHistoryOperationType
    {
        None = 0,
        CashIn,
        CashOut,
        Trade
    }

    /// <summary>
    /// Represents any operation logged to history
    /// </summary>
    public class ApiHistoryOperation
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public ApiCashInHistoryOperation CashIn { get; set; }
        public ApiCashOutHistoryOperation CashOut { get; set; }
        public ApiTradeHistoryOperation Trade { get; set; }

        public static ApiHistoryOperation Create(
            string id,
            DateTime dateTime,
            ApiTradeHistoryOperation trade = null,
            ApiCashInHistoryOperation cashIn = null,
            ApiCashOutHistoryOperation cashout = null)
        {
            return new ApiHistoryOperation
            {
                DateTime = dateTime,
                Id = id,
                CashIn = cashIn,
                CashOut = cashout,
                Trade = trade
            };
        }
    }

    public class ApiBaseCashOperation
    {
        public string Id { get; set; }
        public double Amount { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public string BlockChainHash { get; set; }
        public bool IsRefund { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsSettled { get; set; }
        public string Type { get; set; }
        public string ContextOperationType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStates State { get; set; }
    }

    public class ApiCashInHistoryOperation : ApiBaseCashOperation
    {
        
    }

    public class ApiCashOutHistoryOperation : ApiBaseCashOperation
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ApiCashOutState CashOutState { get; set; }
    }

    public class ApiTradeHistoryOperation
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public bool IsSettled { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
        public string ContextOperationType { get; set; }
        public string State { get; set; }
    }
}