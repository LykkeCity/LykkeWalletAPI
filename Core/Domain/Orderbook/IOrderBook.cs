using System;
using System.Collections.Generic;

namespace Core.Domain.Orderbook
{
    public interface IOrderBook
    {
        string AssetPair { get; }
        bool IsBuy { get; }
        DateTime Timestamp { get; }
        List<VolumePrice> Prices { get; set; }
    }
}
