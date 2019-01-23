using Core.Enumerators;

namespace Core.Exchange
{
    public static class BaseOrderExt
    {
        public const string Buy = "buy";
        public const string Sell = "sell";

        public static OrderAction? GetOrderAction(string actionWord)
        {
            if (!string.IsNullOrEmpty(actionWord))
            {
                if (actionWord.ToLower() == Buy)
                    return OrderAction.Buy;
                if (actionWord.ToLower() == Sell)
                    return OrderAction.Sell;
            }

            return null;
        }
    }
}
