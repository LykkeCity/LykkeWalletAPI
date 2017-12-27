using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Orderbook;

namespace LykkeApi2.Models
{
    public class OrderBookModel
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Levels { get; set; } = new List<VolumePrice>();
    }

    public static class OrderBookModelExtensions
    {
        public static IEnumerable<OrderBookModel> ToApiModel(this IEnumerable<IOrderBook> src)
        {
            return src.Select(orderbook => new OrderBookModel
            {
                AssetPair = orderbook.AssetPair,
                IsBuy = orderbook.IsBuy,
                Timestamp = orderbook.Timestamp,
                Levels = orderbook.Prices
            });
        }
    }
}
