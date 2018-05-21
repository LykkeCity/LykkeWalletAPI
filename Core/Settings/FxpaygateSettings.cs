using System.Collections.Generic;
using Core.Wallet;

namespace Core.Settings
{
    public class FxpaygateSettings
    {
        public string[] Currencies { get; set; }
        public string[] Countries { get; set; }
        public Dictionary<OwnerType, string> ServiceUrls { get; set; }
        public string[] SupportedCurrencies { get; set; }
    }
}