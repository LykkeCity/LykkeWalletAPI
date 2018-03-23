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
    public class CountryPhoneCodesController : Controller
    {
        private readonly ICountryPhoneCodeService _countryPhoneCodeService;

        public CountryPhoneCodesController(ICountryPhoneCodeService countryPhoneCodeService)
        {
            _countryPhoneCodeService = countryPhoneCodeService;
        }

        [HttpGet]
        public ResponseModel<CountriesResponseModel> Get()
        {
            return ResponseModel<CountriesResponseModel>
                .CreateOk(new CountriesResponseModel
                {
                    CountriesList = _countryPhoneCodeService.GetCountries()
                });
        }
    }
}