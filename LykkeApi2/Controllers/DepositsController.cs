using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Services;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.SwiftCredentials.Client;
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
        private readonly ISwiftCredentialsClient _swiftCredentialsClient;
        private readonly IKycStatusService _kycStatusService;
        private readonly IPersonalDataService _personalDataService;
        private readonly IRequestContext _requestContext;

        public DepositsController(
            IPaymentSystemClient paymentSystemService,
            IFeeCalculatorClient feeCalculatorClient,
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IClientDialogsClient clientDialogsClient,
            ISwiftCredentialsClient swiftCredentialsClient,
            IKycStatusService kycStatusService,
            IPersonalDataService personalDataService,
            IRequestContext requestContext)
        {
            _paymentSystemService = paymentSystemService;
            _feeCalculatorClient = feeCalculatorClient;
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _clientDialogsClient = clientDialogsClient;
            _swiftCredentialsClient = swiftCredentialsClient;
            _kycStatusService = kycStatusService;
            _personalDataService = personalDataService;
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
        [Route("swift/{assetId}/requisites")]
        [ProducesResponseType(typeof(SwiftRequisitesRespModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSwiftRequisites([FromRoute] string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);
            
            if(asset == null)
                throw new ClientException(HttpStatusCode.NotFound, ExceptionType.AssetNotFound);
            
            if(!asset.SwiftDepositEnabled || !assetsAvailableToClient.Contains(assetId))
                throw new ClientException(HttpStatusCode.BadRequest, ExceptionType.AssetUnavailable);
            
            var status = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (status != KycStatus.Ok)
                throw new ClientException(ExceptionType.KycRequired);
            
            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);
            
            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw new ClientException(ExceptionType.PendingDialogs);
            
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);
            
            var creds = await _swiftCredentialsClient.GetAsync(personalData.SpotRegulator, assetId);
            
            var assetTitle = asset.DisplayId ?? assetId;

            var clientIdentity = personalData.Email != null ? personalData.Email.Replace("@", ".") : "{1}";
            var purposeOfPayment = string.Format(creds.PurposeOfPayment, assetTitle, clientIdentity);

            if (!purposeOfPayment.Contains(assetId) && !purposeOfPayment.Contains(assetTitle))
                purposeOfPayment += assetTitle;

            if (!purposeOfPayment.Contains(clientIdentity))
                purposeOfPayment += clientIdentity;
            
            return Ok(new SwiftRequisitesRespModel
            {
                AccountName = creds.AccountName,
                AccountNumber = creds.AccountNumber,
                BankAddress = creds.BankAddress,
                Bic = creds.Bic,
                CompanyAddress = creds.CompanyAddress,
                CorrespondentAccount = creds.CorrespondentAccount,
                PurposeOfPayment = purposeOfPayment
            });
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
                throw new ClientException(HttpStatusCode.NotFound, ExceptionType.AssetNotFound);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);

            if (!assetsAvailableToClient.Contains(assetId))
                throw new ClientException(ExceptionType.AssetUnavailable);

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);
            
            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw new ClientException(ExceptionType.PendingDialogs);

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
                throw new ClientException(ExceptionType.AddressNotGenerated);

            return Ok(new CryptoDepositAddressRespModel
                {
                    Address = depositInfo.Address,
                    AddressExtension = depositInfo.AddressExtension,
                    BaseAddress = depositInfo.BaseAddress
                });
        }
    }
}