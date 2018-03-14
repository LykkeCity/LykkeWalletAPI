using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Exchange;
using Core.GlobalSettings;
using LkeServices;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/Setting")]
    [Produces("application/json")]
    [LowerVersion(Devices = "android", LowerVersion = 961)]
    public class SettingController : Controller
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IExchangeSettingsRepository _exchangeSettingsRepository;
        private readonly IRequestContext _requestContext;
        private readonly SrvAssetsHelper _srvAssetsHelper;
        private readonly IAppGlobalSettingsRepository _appGlobalSettingsRepository;
        private readonly IClientAccountClient _clientAccountClient;

        public SettingController(IFeeCalculatorClient feeCalculatorClient,
            IRequestContext requestContext,
            SrvAssetsHelper srvAssetsHelper,
            IExchangeSettingsRepository exchangeSettingsRepository,
            IAppGlobalSettingsRepository appGlobalSettingsRepository,
            IClientAccountClient clientAccountClient)
        {
            _feeCalculatorClient = feeCalculatorClient ?? throw new ArgumentNullException(nameof(feeCalculatorClient));
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
            return Ok(await GetAppSettingsAsync(isIosDevice));
        }

        private async Task<AppSettingsModel> GetAppSettingsAsync(bool isIosDevice)
        {
            var clientId = _requestContext.ClientId;
            var partnerId = _requestContext.PartnerId;

            var settings = await _exchangeSettingsRepository.GetOrDefaultAsync(clientId);
            var asset = await _srvAssetsHelper.GetBaseAssetForClient(clientId, isIosDevice, partnerId);
            var clientAppSettings = await _appGlobalSettingsRepository.GetFromDbOrDefault();
            var refundSettings = await _clientAccountClient.GetRefundAddressAsync(clientId);
            var fee = new ApiFee
            {
                BankCardsFeeSizePercentage = (await _feeCalculatorClient.GetBankCardFees()).Percentage,
                CashOut = (await _feeCalculatorClient.GetCashoutFeesAsync()).Select(cashoutFee =>
                    new CashoutFee
                    {
                        AssetId = cashoutFee.AssetId,
                        Size = cashoutFee.Size,
                        Type = cashoutFee.Type
                    }).ToList()
            };
            return settings.ConvertToApiModel(asset, clientAppSettings, refundSettings, fee);
        }
    }
}