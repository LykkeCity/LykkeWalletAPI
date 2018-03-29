using System.Threading.Tasks;
using Core.Countries;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class DictionaryController : Controller
    {
        private readonly ICountryPhoneCodeService _countryPhoneCodeService;

        public DictionaryController(ICountryPhoneCodeService countryPhoneCodeService)
        {
            _countryPhoneCodeService = countryPhoneCodeService;
        }

        [HttpGet]
        public ResponseModel<CountriesResponseModel> GetCountryPhoneCodes()
        {
            return ResponseModel<CountriesResponseModel>
                .CreateOk(new CountriesResponseModel
                {
                    CountriesList = _countryPhoneCodeService.GetCountries()
                });
        }
    }
}