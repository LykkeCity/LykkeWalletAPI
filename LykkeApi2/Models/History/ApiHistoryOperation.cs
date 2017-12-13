using LykkeApi2.Models.Operations;
using System;

namespace LykkeApi2.Models.History
{
    /// <summary>
    /// Represents anny operation logged to history
    /// </summary>
    public class ApiHistoryOperation
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public ApiBalanceChange CashInOut { get; set; }
        public ApiTrade Trade { get; set; }
        public ApiTransfer Transfer { get; set; }
        public ApiCashOutAttempt CashOutAttempt { get; set; }
        public ApiCashOutCancelled CashOutCancelled { get; set; }
        public ApiCashOutDone CashOutDone { get; set; }
        public ApiLimitTradeEvent LimitTradeEvent { get; set; }

        public static ApiHistoryOperation Create(
            string id,
            DateTime dateTime,
            ApiTrade trade = null,
            ApiBalanceChange cashInOut = null, 
            ApiTransfer transfer = null, 
            ApiCashOutAttempt cashOutAttempt = null,
            ApiCashOutCancelled cashOutCancelled = null, 
            ApiCashOutDone apiCashOutDone = null,
            ApiLimitTradeEvent limitTradeEvent = null)
        {
            return new ApiHistoryOperation
            {
                Id = id,
                DateTime = dateTime,
                CashInOut = cashInOut,
                Trade = trade,
                Transfer = transfer,
                CashOutAttempt = cashOutAttempt,
                CashOutCancelled = cashOutCancelled,
                CashOutDone = apiCashOutDone,
                LimitTradeEvent = limitTradeEvent
            };
        }
    }
}