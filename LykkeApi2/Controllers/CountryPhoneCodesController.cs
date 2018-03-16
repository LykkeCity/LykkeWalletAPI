using System.Linq;
using System.Threading.Tasks;
using Core.Countries;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CountryPhoneCodesController : Controller
    {
        private readonly IPersonalDataService _personalDataService;
        private readonly IRequestContext _requestContext;
        private readonly ICountryPhoneCodeService _countryPhoneCodeService;

        public CountryPhoneCodesController(IPersonalDataService personalDataService, 
            IRequestContext requestContext, ICountryPhoneCodeService countryPhoneCodeService)
        {
            _personalDataService = personalDataService;
            _requestContext = requestContext;
            _countryPhoneCodeService = countryPhoneCodeService;
        }

        [HttpGet]
        public async Task<ResponseModel<CountriesResponseModel>> Get()
        {
            var clientId = _requestContext.ClientId;

            var country = (await _personalDataService.GetAsync(clientId)).Country;

            if (string.IsNullOrEmpty(country))
                country = "CHE";

            return ResponseModel<CountriesResponseModel>
                .CreateOk(new CountriesResponseModel
                {
                    Current = country,
                    CountriesList = _countryPhoneCodeService.GetCountries()
                });
        }
    }
}