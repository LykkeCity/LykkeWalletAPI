using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Client
{
    public class ZipCodeModel
    {
        [Required]
        public string Zip { get; set; }
    }
}