using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.ClientAccountModels
{
    public class IndexByPhoneResponseModel
    {
        public string PhoneNumber { get; set; }
        public string ClientId { get; set; }
        public string PreviousPhoneNuber { get; set; }
    }
}
