using System;
using System.Collections.Generic;
using System.Linq;
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

    public static class OrderBookModelsExtensions
    {
        public static IEnumerable<OrderBookModel> ToApiModel(this IEnumerable<IOrderBook> src)
        {
            return src.Select(orderbook => new OrderBookModel
            {
                AssetPair = orderbook.AssetPair,
                IsBuy = orderbook.IsBuy,
                Timestamp = orderbook.Timestamp,
                Levels = orderbook.Prices.ProcessPrices()
            });
        }

        public static List<VolumePrice> ProcessPrices(this IEnumerable<VolumePrice> prices)
        {
            return prices.Select(price => new VolumePrice
            {
                ClientId = price.ClientId,
                Id = price.Id,
                Price = price.Price,
                Volume = Math.Abs(price.Volume)
            }).ToList();
        }
    }
}
