namespace Core.Exchange
{
    public class MatchedLimitOrder : MatchedOrder
    {
        public double Price { get; set; }

        public static MatchedLimitOrder Create(ILimitOrder limitOrder, double volume)
        {
            return new MatchedLimitOrder
            {
                Price = limitOrder.Price,
                Id = limitOrder.Id,
                Volume = volume
            };
        }
    }
}
