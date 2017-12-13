using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Pledges
{
    public class CreatePledgeRequest
    {
        [Required]
        public int CO2Footprint { get; set; }
        [Required]
        public int ClimatePositiveValue { get; set; }
    }
}
