using System;

namespace Core.Exchange
{
    public interface IMarketOrder : IOrderBase
    {
        DateTime MatchedAt { get; }
    }
}
