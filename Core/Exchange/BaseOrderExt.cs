using Core.Enumerators;

namespace Core.Exchange
{
    public static class BaseOrderExt
    {
        public const string Buy = "buy";
        public const string Sell = "sell";

        public static OrderAction OrderAction(this IOrderBase orderBase)
        {
            return orderBase.Volume > 0 ? Enumerators.OrderAction.Buy : Enumerators.OrderAction.Sell;
        }

        public static OrderAction? GetOrderAction(string actionWord)
        {
            if (actionWord.ToLower() == Buy)
                return Enumerators.OrderAction.Buy;
            if (actionWord.ToLower() == Sell)
                return Enumerators.OrderAction.Sell;

            return null;
        }
    }
}