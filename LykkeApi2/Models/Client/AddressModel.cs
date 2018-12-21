using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Client
{
    public class AddressModel
    {
        [Required]
        public string Address { get; set; }
    }
}