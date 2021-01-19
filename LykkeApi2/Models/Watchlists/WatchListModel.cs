using System.Collections.Generic;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Core.Domain;

namespace LykkeApi2.Models.Watchlists
{
    public class WatchListModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> AssetPairIds { get; set; }
        public int Order { get; set; }
        public bool ReadOnlyProperty { get; set; }
    }

    public static class WatchListModelHelper
    {
        public static WatchListModel ToApiModel(this IWatchList watchList)
        {
            return new WatchListModel
            {
                Id = watchList.Id,
                Name = watchList.Name,
                Order = watchList.Order,
                ReadOnlyProperty = watchList.ReadOnly,
                AssetPairIds = watchList.AssetIds ?? new List<string>()
            };
        }
    }
}
