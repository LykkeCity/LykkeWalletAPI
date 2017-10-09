using LykkeApi2.Models.ApiContractModels;

namespace LykkeApi2.Models.TransactionHistoryModels
{
    public class TransactionsResponseModel
    {
        public ApiBalanceChangeModel[] CashInOut { get; set; }
        public ApiTradeOperation[] Trades { get; set; }
        public ApiTransfer[] Transfers { get; set; }

        public ApiCashOutAttempt[] CashOutAttempts { get; set; }
        public ApiCashOutCancelled[] CashOutCancelled { get; set; }
        public ApiCashOutDone[] CashOutDone { get; set; }

        public ApiLimitTradeEvent[] LimitTradeEvents { get; set; }

        public static TransactionsResponseModel Create(ApiTradeOperation[] tradeOperations,
            ApiBalanceChangeModel[] cashInOut, ApiTransfer[] transfers, ApiCashOutAttempt[] cashOutAttempts,
            ApiCashOutCancelled[] cashOutCancelled, ApiCashOutDone[] apiCashOutDone, ApiLimitTradeEvent[] limitTradeEvents)
        {
            return new TransactionsResponseModel
            {
                CashInOut = cashInOut,
                Trades = tradeOperations,
                Transfers = transfers,
                CashOutAttempts = cashOutAttempts,
                CashOutCancelled = cashOutCancelled,
                CashOutDone = apiCashOutDone,
                LimitTradeEvents = limitTradeEvents
            };
        }
    }
}
