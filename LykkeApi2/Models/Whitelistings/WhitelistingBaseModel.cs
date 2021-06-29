using System;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Whitelistings
{
    public class WhitelistingBaseModel
    {
        public string Name { set; get; }
        public string AddressBase { set; get; }
        public string AddressExtension { set; get; }
    }
}
