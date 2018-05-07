using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Settings.Client;
using Lykke.Service.Settings.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [LowerVersion(Devices = "android", LowerVersion = 961)]
    public class AppSettingController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly ISettingsClient _settingsService;

        public AppSettingController(
            IRequestContext requestContext,
            ISettingsClient settingsService)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _settingsService = settingsService;
        }

        [HttpGet]
        [SwaggerOperation("Get")]
        [ProducesResponseType(typeof(AppSettingsResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var isIosDevice = _requestContext.IsIosDevice;
            var clientId = _requestContext.ClientId;
            var partnerId = _requestContext.PartnerId;

            var result = await _settingsService.GetAppSettingsAsync(isIosDevice,clientId, partnerId);

            return Ok(result);
        }
    }
}