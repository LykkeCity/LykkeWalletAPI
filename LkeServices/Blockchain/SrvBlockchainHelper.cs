using System;
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
            try
            {
                return BitcoinAddress.Create(baseAddress).ToColoredAddress().ToString();
            }
            //unable to convert to colored address
            catch (Exception)
            {
                return baseAddress;
            }
        }
    }
}
