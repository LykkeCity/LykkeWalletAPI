using System;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Client
{
    public class TradingModel
    {
        [Required]
        public int Ttl { get; set; }
    }
}