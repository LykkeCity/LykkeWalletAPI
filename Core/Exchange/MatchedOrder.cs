namespace Core.Exchange
{
    public class MatchedOrder
    {
        public string Id { get; set; }
        public double Volume { get; set; }

        internal static MatchedOrder Create(IOrderBase orderBase, double volume)
        {
            return new MatchedOrder
            {
                Id = orderBase.Id,
                Volume = volume
            };
        }
    }
}
