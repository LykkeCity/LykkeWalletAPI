using System.Threading.Tasks;
using Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ConfigsController : Controller
    {
        private readonly CreditVouchersSettings _creditVouchersSettings;

        public ConfigsController(CreditVouchersSettings creditVouchersSettings)
        {
            _creditVouchersSettings = creditVouchersSettings;
        }

        [HttpGet]
        [SwaggerOperation("Get")]
        public async Task<IActionResult> Get()
        {
            return Ok(_creditVouchersSettings);
        }
    }
}