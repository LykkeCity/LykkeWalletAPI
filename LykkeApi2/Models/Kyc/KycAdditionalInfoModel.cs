using System;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Kyc
{
    public class KycAdditionalInfoModel
    {
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Zip { get; set; }
    }
}
