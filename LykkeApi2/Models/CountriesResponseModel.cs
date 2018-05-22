using System.Collections.Generic;
using Core.Countries;

namespace LykkeApi2.Models
{
    public class CountriesResponseModel
    {
        public IEnumerable<CountryItem> CountriesList { get; set; }
    }
}