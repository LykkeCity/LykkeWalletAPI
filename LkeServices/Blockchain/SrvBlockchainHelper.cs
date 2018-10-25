using Core.Services;
using NBitcoin;

namespace LkeServices.Blockchain
{
    public class SrvBlockchainHelper: ISrvBlockchainHelper
    {
        public string GenerateColoredAddress(string baseAddress)
        {
            if (baseAddress == null)
                return null;
            return BitcoinAddress.Create(baseAddress).ToColoredAddress().ToString();
        }
    }
}
