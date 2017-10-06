using System;

namespace Core.Exchange
{
    public enum OrderAction
    {
        Buy, Sell
    }

    public enum OrderStatus
    {
        Ok = 0,
        LowBalance = 401,
        AlreadyProcessed = 402,
        UnknownAsset = 410,
        NoLiquidity = 411,
        NotEnoughFunds = 412,
        Dust = 413,
        Runtime = 500
    }

    public interface IOrderBase
    {
        string Id { get; }
        string ClientId { get; set; }
        DateTime CreatedAt { get; set; }
        double Volume { get; set; }
        double Price { get; set; }
        string AssetPairId { get; set; }
        string Status { get; set; }
        bool Straight { get; set; }
    }

    public static class BaseOrderExt
    {
        public const string Buy = "buy";
        public const string Sell = "sell";

        public static OrderAction OrderAction(this IOrderBase orderBase)
        {
            return orderBase.Volume > 0 ? Exchange.OrderAction.Buy : Exchange.OrderAction.Sell;
        }

        public static OrderAction? GetOrderAction(string actionWord)
        {
            if (actionWord.ToLower() == Buy)
                return Exchange.OrderAction.Buy;
            if (actionWord.ToLower() == Sell)
                return Exchange.OrderAction.Sell;

            return null;
        }

        public static OrderAction ViceVersa(this OrderAction orderAction)
        {
            if (orderAction == Exchange.OrderAction.Buy)
                return Exchange.OrderAction.Sell;
            return Exchange.OrderAction.Buy;
        }
    }
}