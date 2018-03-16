using System.Collections.Generic;
using Core.Countries;

namespace LykkeApi2.Models
{
    public class CountriesResponseModel
    {
        public string Current { get; set; }
        public IEnumerable<CountryItem> CountriesList { get; set; }
    }
}