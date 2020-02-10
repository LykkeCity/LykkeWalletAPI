namespace LykkeApi2.Models.Markets
{
    /// <summary>
    /// A model representing the current state of the Spot (only) market in terms of a particular asset pair.
    /// </summary>
    public class MarketSlice
    {
        /// <summary>
        /// Asset pair ID.
        /// </summary>
        public string AssetPair { get; set; }
        /// <summary>
        /// Trading volume for the current day. Is obtained from asset pair's Day Trade candle for today.
        /// </summary>
        public decimal Volume24H { get; set; }
        /// <summary>
        /// Trade price change for the current day. Is calculated by the formula: (Close - Open) / Open, where Open and Close are the corresponding prices for today's Day Trade candle.
        /// </summary>
        public decimal PriceChange24H { get; set; }
        /// <summary>
        /// The last trade price. Is obtained from asset pair's Day Trade candle for today and is equal to its Close price.
        /// </summary>
        public decimal LastPrice { get; set; }
        /// <summary>
        /// The actual Bid price for the asset pair.
        /// </summary>
        public decimal Bid { get; set; }
        /// <summary>
        /// The actual Ask price for the asset pair.
        /// </summary>
        public decimal Ask { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
    }
}
