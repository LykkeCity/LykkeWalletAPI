using System.Collections.Generic;
using System.Net;
using Core.Countries;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class CatalogsController : Controller
    {
        private readonly ICountryPhoneCodeService _countryPhoneCodeService;

        public CatalogsController(ICountryPhoneCodeService countryPhoneCodeService)
        {
            _countryPhoneCodeService = countryPhoneCodeService;
        }

        [HttpGet]
        [Route("countries")]
        [SwaggerOperation("GetCountryPhoneCodes")]
        [ProducesResponseType(typeof(IEnumerable<CountryItem>), (int)HttpStatusCode.OK)]
        public IActionResult GetCountryPhoneCodes()
        {
            return Ok(_countryPhoneCodeService.GetCountries());
        }
    }
}