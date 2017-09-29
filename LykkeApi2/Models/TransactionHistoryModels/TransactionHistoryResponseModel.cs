using LykkeApi2.Models.ApiContractModels;
using System;

namespace LykkeApi2.Models.TransactionHistoryModels
{
    public class TransactionHistoryResponseModel
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

        public static TransactionHistoryResponseModel Create(DateTime dateTime, string id, ApiTradeOperation tradeOperation = null,
            ApiBalanceChangeModel cashInOut = null, ApiTransfer transfer = null, ApiCashOutAttempt cashOutAttempt = null,
            ApiCashOutCancelled cashOutCancelled = null, ApiCashOutDone apiCashOutDone = null, ApiLimitTradeEvent limitTradeEvent = null)
        {
            return new TransactionHistoryResponseModel
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
}
