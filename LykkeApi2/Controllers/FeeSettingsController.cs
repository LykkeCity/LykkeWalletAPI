using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [LowerVersion(Devices = "android", LowerVersion = 961)]
    public class FeeSettingController : Controller
    {
        private readonly IFeeCalculatorClient _feeCalculatorClient;

        public FeeSettingController(IFeeCalculatorClient feeCalculatorClient)
        {
            _feeCalculatorClient = feeCalculatorClient ?? throw new ArgumentNullException(nameof(feeCalculatorClient));
        }

        [HttpGet]
        public async Task<IActionResult> Get(string assetId = null)
        {
            var fee = new ApiFeeSettingsModel
            {
                BankCardsFeeSizePercentage = (await _feeCalculatorClient.GetBankCardFees()).Percentage,
                CashOut = string.IsNullOrEmpty(assetId)
                    ? (await _feeCalculatorClient.GetCashoutFeesAsync()).Select(cashoutFee =>
                        new CashoutFee
                        {
                            AssetId = cashoutFee.AssetId,
                            Size = cashoutFee.Size,
                            Type = cashoutFee.Type
                        }).ToList()
                    : (await _feeCalculatorClient.GetCashoutFeesAsync(assetId)).Select(cashoutFee =>
                        new CashoutFee
                        {
                            AssetId = cashoutFee.AssetId,
                            Size = cashoutFee.Size,
                            Type = cashoutFee.Type
                        }).ToList()
            };
            return Ok(fee);
        }
    }
}