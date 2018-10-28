using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    //TODO remove srv, srv usages and  NBitcoin library when color coins become obsolete
    public interface ISrvBlockchainHelper
    {
        string GenerateColoredAddress(string baseAddress);
    }
}
