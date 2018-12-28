using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Core.Services;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.SwiftCredentials.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.Deposits;
using LykkeApi2.Models.Fees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
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
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly IRequestContext _requestContext;
        private readonly ISrvBlockchainHelper _srvBlockchainHelper;
        private readonly ISet<string> _coloredAssetIds;

        public DepositsController(
            IPaymentSystemClient paymentSystemService,
            IFeeCalculatorClient feeCalculatorClient,
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IClientDialogsClient clientDialogsClient,
            ISwiftCredentialsClient swiftCredentialsClient,
            IKycStatusService kycStatusService,
            IPersonalDataService personalDataService,
            ILimitationsServiceClient limitationsServiceClient,
            IRequestContext requestContext, 
            ISrvBlockchainHelper srvBlockchainHelper)
        {
            _paymentSystemService = paymentSystemService;
            _feeCalculatorClient = feeCalculatorClient;
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _clientDialogsClient = clientDialogsClient;
            _swiftCredentialsClient = swiftCredentialsClient;
            _kycStatusService = kycStatusService;
            _personalDataService = personalDataService;
            _limitationsServiceClient = limitationsServiceClient;
            _requestContext = requestContext;
            _srvBlockchainHelper = srvBlockchainHelper;

            _coloredAssetIds = new[]
            {
                LykkeConstants.LykkeAssetId,
                LykkeConstants.LykkeForwardAssetId,
                LykkeConstants.HcpAssetId
            }.ToHashSet();
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

        [HttpPost]
        [Route("swift/{assetId}/email")]
        public async Task<IActionResult> PostRequestSwiftRequisites([FromRoute] string assetId, [FromBody] SwiftDepositEmailModel model)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);
            
            if(asset == null)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            
            if(!asset.SwiftDepositEnabled || !assetsAvailableToClient.Contains(assetId))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);
            
            if(model.Amount <= 0 || model.Amount != decimal.Round(model.Amount, asset.Accuracy))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);
            
            var status = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (status != KycStatus.Ok)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);
            
            var checkResult = await _limitationsServiceClient.CheckAsync(
                _requestContext.ClientId,
                assetId,
                decimal.ToDouble(model.Amount),
                CurrencyOperationType.SwiftTransfer);
            
            if (!checkResult.IsValid)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.DepositLimitReached);
            
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);

            _swiftCredentialsClient.EmailRequestAsync(
                _requestContext.ClientId,
                personalData.SpotRegulator,
                assetId,
                model.Amount);

            return Ok();
        }

        [HttpGet]
        [Route("swift/{assetId}/requisites")]
        [ProducesResponseType(typeof(SwiftRequisitesRespModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSwiftRequisites([FromRoute] string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);
            
            if(asset == null)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            
            if(!asset.SwiftDepositEnabled || !assetsAvailableToClient.Contains(assetId))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);
            
            var status = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (status != KycStatus.Ok)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);
            
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);
            
            var creds = await _swiftCredentialsClient.GetForClientAsync(_requestContext.ClientId, personalData.SpotRegulator, assetId);
            
            return Ok(new SwiftRequisitesRespModel
            {
                AccountName = creds.AccountName,
                AccountNumber = creds.AccountNumber,
                BankAddress = creds.BankAddress,
                Bic = creds.Bic,
                CompanyAddress = creds.CompanyAddress,
                CorrespondentAccount = creds.CorrespondentAccount,
                PurposeOfPayment = creds.PurposeOfPayment
            });
        }
        
        [HttpPost]
        [Route("crypto/{assetId}/address")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PostCryptosDepositAddresses([FromRoute] string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);

            if (asset == null || asset.IsDisabled || !asset.BlockchainDepositEnabled)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);

            if (!assetsAvailableToClient.Contains(assetId))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);
            
            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.PendingDialogs);
            
            var status = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (status != KycStatus.Ok)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);

            var isFirstGeneration = string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId);

            if (isFirstGeneration)
            {
                await _blockchainWalletsClient.CreateWalletAsync(
                    "first-generation-blockchain",
                    assetId,
                    Guid.Parse(_requestContext.ClientId));
            }
            else
            {
                await _blockchainWalletsClient.CreateWalletAsync(
                    asset.BlockchainIntegrationLayerId,
                    asset.BlockchainIntegrationLayerAssetId,
                    Guid.Parse(_requestContext.ClientId));
            }

            return Ok();
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
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            
            var assetsAvailableToClient =
                await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);

            if (!assetsAvailableToClient.Contains(assetId))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);
            
            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.PendingDialogs);
            
            var status = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (status != KycStatus.Ok)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);
            
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
                throw LykkeApiErrorException.BadRequest(
                    LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressNotGenerated);

            var depositAddress = depositInfo.Address;
            if (_coloredAssetIds.Contains(asset.BlockchainIntegrationLayerAssetId))
            {
                depositAddress = _srvBlockchainHelper.GenerateColoredAddress(depositInfo.Address);
            }

            return Ok(new CryptoDepositAddressRespModel
                {
                    Address = depositAddress,
                    AddressExtension = depositInfo.AddressExtension,
                    BaseAddress = depositInfo.BaseAddress
            });
        }
    }
}