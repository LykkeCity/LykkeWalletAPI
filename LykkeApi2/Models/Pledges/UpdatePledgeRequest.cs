using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Pledges
{
    public class UpdatePledgeRequest
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string ClientId { get; set; }
        [Required]
        public int CO2Footprint { get; set; }
        [Required]
        public int ClimatePositiveValue { get; set; }
    }
}
