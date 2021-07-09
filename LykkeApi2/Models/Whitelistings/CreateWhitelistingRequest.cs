using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Whitelistings
{
    public class CreateWhitelistingRequest : WhitelistingBaseModel
    {
        public string Code2Fa { get; set; }
        public string AssetId { set; get; }
        public string WalletId { set; get; }
    }
}
