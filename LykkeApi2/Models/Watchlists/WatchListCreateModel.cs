using System.Collections.Generic;

namespace LykkeApi2.Models.Watchlists
{
    public class WatchListCreateModel
    {
        public string Name { get; set; }

        public int Order { get; set; }

        public IEnumerable<string> AssetIds { get; set; }
    }
}
