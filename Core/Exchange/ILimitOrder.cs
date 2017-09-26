namespace Core.Exchange
{
    public interface ILimitOrder : IOrderBase
    {
        double RemainingVolume { get; set; }
        string MatchingId { get; set; }
    }
}
