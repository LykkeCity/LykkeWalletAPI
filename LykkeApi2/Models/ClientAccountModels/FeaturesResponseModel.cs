using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Session.Contracts;

namespace LykkeApi2.Models.ClientAccountModels
{
    public class FeaturesResponseModel
    {
        public bool AffiliateEnabled { get; set; }

        public TradingSessionModel TradingSession { get; set; }
    }
}
