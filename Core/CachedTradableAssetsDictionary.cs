using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Assets.Client.Models;

namespace Core
{
    public class CachedTradableAssetsDictionary : CachedDataDictionary<string, Asset>
    {
        public CachedTradableAssetsDictionary(Func<Task<Dictionary<string, Asset>>> getData, int validDataInSeconds = 300)
            : base(getData, validDataInSeconds)
        {
        }
    }
}