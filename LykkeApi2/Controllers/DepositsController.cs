using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.Fees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DepositsController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemService;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IClientDialogsClient _clientDialogsClient;
        private readonly IRequestContext _requestContext;

        public DepositsController(
            IPaymentSystemClient paymentSystemService,
            IFeeCalculatorClient feeCalculatorClient,
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IRequestContext requestContext)
        {
            _paymentSystemService = paymentSystemService;
            _feeCalculatorClient = feeCalculatorClient;
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get last PaymentTransaction
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("fxpaygate/last")]
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLastFxPaygate()
        {
            var result = await _paymentSystemService.GetLastByDateAsync(_requestContext.ClientId);
            return Ok(result);
        }

        /// <summary>
        /// Get fee amount
        /// </summary>
        /// <returns>Fee amount</returns>
        [HttpGet]
        [Route("fxpaygate/fee")]
        [ProducesResponseType(typeof(FxPaygateFeeModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFxPaygateFee()
        {
            return Ok(
                new FxPaygateFeeModel
                {
                    Amount = (await _feeCalculatorClient.GetBankCardFees()).Percentage
                });
        }

        /// <summary>
        /// Get Url for PaymentSystem
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns with Url for PaymentSystem</returns>
        [HttpPost]
        [Route("fxpaygate")]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(FxPaygatePaymentUrlResponseModel), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> PostFxPaygate([FromBody] FxPaygatePaymentUrlRequestModel input)
        {
            var result = await _paymentSystemService.GetUrlDataAsync(
                _requestContext.ClientId,
                input.Amount,
                input.AssetId,
                input.WalletId,
                input.FirstName,
                input.LastName,
                input.City,
                input.Zip,
                input.Address,
                input.Country,
                input.Email,
                input.Phone,
                DepositOption.BankCard,
                input.OkUrl,
                input.FailUrl,
                input.CancelUrl);

            var resp = new FxPaygatePaymentUrlResponseModel
            {
                Url = result.Url,
                CancelUrl = result.CancelUrl,
                FailUrl = result.FailUrl,
                OkUrl = result.OkUrl
            };

            return Ok(resp);
        }

        [HttpGet]
        [Route("crypto/{assetId}/address")]
        [ProducesResponseType(typeof(CryptoDepositAddressRespModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCryptosDepositAddresses([FromRoute] string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);

            if (asset == null || asset.IsDisabled || !asset.BlockchainDepositEnabled)
                return NotFound();
            
            var assetsAvailableToClient =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            if (!assetsAvailableToClient.Contains(assetId))
                return BadRequest();

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);
            
            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                return StatusCode(412);

            var isFirstGeneration = string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId);

            var depositInfo =
                isFirstGeneration
                    ? await _blockchainWalletsClient.TryGetAddressAsync(
                        "first-generation-blockchain",
                        assetId,
                        Guid.Parse(_requestContext.ClientId))
                    : await _blockchainWalletsClient.TryGetAddressAsync(
                        asset.BlockchainIntegrationLayerId,
                        asset.BlockchainIntegrationLayerAssetId,
                        Guid.Parse(_requestContext.ClientId));

            if (depositInfo == null)
                return NotFound();

            return Ok(new CryptoDepositAddressRespModel
                {
                    Address = depositInfo.Address,
                    AddressExtension = depositInfo.AddressExtension,
                    BaseAddress = depositInfo.BaseAddress
                });
        }
    }
}