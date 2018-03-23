using System;
using System.Threading.Tasks;
using Core.Exchange;
using Core.GlobalSettings;
using LkeServices;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [LowerVersion(Devices = "android", LowerVersion = 961)]
    public class AppSettingController : Controller
    {
        private readonly IExchangeSettingsRepository _exchangeSettingsRepository;
        private readonly IRequestContext _requestContext;
        private readonly SrvAssetsHelper _srvAssetsHelper;
        private readonly IAppGlobalSettingsRepository _appGlobalSettingsRepository;
        private readonly IClientAccountClient _clientAccountClient;

        public AppSettingController(
            IRequestContext requestContext,
            SrvAssetsHelper srvAssetsHelper,
            IExchangeSettingsRepository exchangeSettingsRepository,
            IAppGlobalSettingsRepository appGlobalSettingsRepository,
            IClientAccountClient clientAccountClient)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _srvAssetsHelper = srvAssetsHelper;
            _exchangeSettingsRepository = exchangeSettingsRepository;
            _appGlobalSettingsRepository = appGlobalSettingsRepository;
            _clientAccountClient = clientAccountClient;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var isIosDevice = _requestContext.IsIosDevice;
            var clientId = _requestContext.ClientId;
            var partnerId = _requestContext.PartnerId;

            var settings = await _exchangeSettingsRepository.GetOrDefaultAsync(clientId);
            var assetId = await _srvAssetsHelper.GetBaseAssetIdForClient(clientId, isIosDevice, partnerId);
            var clientAppSettings = await _appGlobalSettingsRepository.GetFromDbOrDefault();
            var refundSettings = await _clientAccountClient.GetRefundAddressAsync(clientId);

            return Ok(settings.ConvertToAppSettingsModel(assetId, clientAppSettings, refundSettings));
        }
    }
}