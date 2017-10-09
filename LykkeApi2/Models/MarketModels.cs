using Lykke.Service.RateCalculator.Client.AutorestClient.Models;

namespace LykkeApi2.Models
{
    public class ConvertionRequest
    {
        public string BaseAssetId { get; set; }
        public AssetWithAmount[] AssetsFrom { get; set; }

        public string OrderAction { get; set; }
    }

    public class ConvertionResponse
    {
        public ConversionResult[] Converted { get; set; }
    }
}