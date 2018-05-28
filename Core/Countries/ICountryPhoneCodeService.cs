using System;
using System.Collections.Generic;

namespace Core.Countries
{
    public interface ICountryPhoneCodeService
    {
        DateTime LastModified { get; }

        IEnumerable<CountryItem> GetCountries();
    }
}