using System;

namespace LykkeApi2.Models.Whitelistings
{
    public class WhitelistingResponseModel : WhitelistingModel
    {
        public string WalletName { set; get; }
        public string AssetName { set; get; }
        public DateTime StartsAt { set; get; }
    }
}
