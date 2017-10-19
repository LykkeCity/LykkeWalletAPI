using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.Pledges
{
    public class UpdatePledgeResponse
    {
        public string Id { get; set; }
        public int CO2Footprint { get; set; }
        public int ClimatePositiveValue { get; set; }
    }
}
