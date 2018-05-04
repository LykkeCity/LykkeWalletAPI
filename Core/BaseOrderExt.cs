using Core.Enumerators;

namespace Core
{
    public static class BaseOrderExt
    {
        public const string Buy = "buy";
        public const string Sell = "sell";
        
        public static OrderAction? GetOrderAction(string actionWord)
        {
            if (actionWord.ToLower() == Buy)
                return OrderAction.Buy;
            if (actionWord.ToLower() == Sell)
                return OrderAction.Sell;

            return null;
        }
    }
}