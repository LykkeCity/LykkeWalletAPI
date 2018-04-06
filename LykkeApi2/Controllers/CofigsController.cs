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

        public ConfigsController(PaymentSystemsSettings paymentSystemsSettings)
        {
            _creditVouchersSettings = paymentSystemsSettings.CreditVouchers;
        }

        [HttpGet]
        [SwaggerOperation("Get")]
        public async Task<IActionResult> Get()
        {
            return Ok(_creditVouchersSettings);
        }
    }
}