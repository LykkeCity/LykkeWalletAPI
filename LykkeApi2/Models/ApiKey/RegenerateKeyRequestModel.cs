using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.ApiKey
{
    public class RegenerateKeyRequestModel
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Code { get; set; }
    }
}
