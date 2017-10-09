namespace LykkeApi2.Models.ApiContractModels
{
    public class ApiLimitOrdersAndTrades
    {
        public ApiLimitOrder LimitOrder { get; set; }
        public ApiTradeOperation [] Trades { get; set; }
    }
}
