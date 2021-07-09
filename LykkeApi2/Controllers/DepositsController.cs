using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Blockchain;
using Core.Constants;
using Core.Services;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Payments.Link4Pay;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.FeeCalculator.Client;
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
        private readonly Link4PayService.Link4PayServiceClient _link4PayServiceClient;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IClientDialogsClient _clientDialogsClient;
        private readonly ISwiftCredentialsClient _swiftCredentialsClient;
        private readonly IKycStatusService _kycStatusService;
        private readonly IPersonalDataService _personalDataService;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IRequestContext _requestContext;
        private readonly ISrvBlockchainHelper _srvBlockchainHelper;
        private readonly ISiriusWalletsService _siriusWalletsService;
        private readonly ISet<string> _coloredAssetIds;

        public DepositsController(
            IPaymentSystemClient paymentSystemService,
            Link4PayService.Link4PayServiceClient link4PayServiceClient,
            IFeeCalculatorClient feeCalculatorClient,
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IClientDialogsClient clientDialogsClient,
            ISwiftCredentialsClient swiftCredentialsClient,
            IKycStatusService kycStatusService,
            IPersonalDataService personalDataService,
            ILimitationsServiceClient limitationsServiceClient,
            IClientAccountClient clientAccountClient,
            IRequestContext requestContext,
            ISrvBlockchainHelper srvBlockchainHelper,
            ISiriusWalletsService siriusWalletsService)
        {
            _paymentSystemService = paymentSystemService;
            _link4PayServiceClient = link4PayServiceClient;
            _feeCalculatorClient = feeCalculatorClient;
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _clientDialogsClient = clientDialogsClient;
            _swiftCredentialsClient = swiftCredentialsClient;
            _kycStatusService = kycStatusService;
            _personalDataService = personalDataService;
            _limitationsServiceClient = limitationsServiceClient;
            _clientAccountClient = clientAccountClient;
            _requestContext = requestContext;
            _srvBlockchainHelper = srvBlockchainHelper;
            _siriusWalletsService = siriusWalletsService;

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
        [Obsolete("use /api/deposits/last")]
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]
        public Task<IActionResult> GetLastFxPaygate()
        {
            return GetLastTransaction();
        }

        /// <summary>
        /// Get last PaymentTransaction
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("last")]
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLastTransaction()
        {
            var result = await _paymentSystemService.GetLastByDateAsync(_requestContext.ClientId);
            return Ok(result);
        }

        /// <summary>
        /// Get transaction information
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("{transactionId}")]
        [ProducesResponseType(typeof(TransactionInfoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetTransactionInfo(string transactionId)
        {
            TransactionInfoResponse result =
                await _link4PayServiceClient.GetTransactionInfoAsync(new TransactionRequest
                {
                    TransactionId = transactionId
                });

            return string.IsNullOrEmpty(result.AssetId)
                ? (IActionResult) NotFound()
                : Ok(result);
        }

        /// <summary>
        /// Get fee amount
        /// </summary>
        /// <returns>Fee amount</returns>
        [HttpGet]
        [Route("fxpaygate/fee")]
        [Obsolete("use /api/deposits/bankCard/fee")]
        [ProducesResponseType(typeof(BankCardFeeModel), (int)HttpStatusCode.OK)]
        public Task<IActionResult> GetFxPaygateFee()
        {
            return GetBankCardFee();
        }

        /// <summary>
        /// Get fee amount
        /// </summary>
        /// <returns>Fee amount</returns>
        [HttpGet]
        [Route("bankCard/fee")]
        [ProducesResponseType(typeof(BankCardFeeModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetBankCardFee()
        {
            return Ok(
                new BankCardFeeModel
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
        [Obsolete("use /api/deposits/paymentUrl")]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(PaymentUrlResponseModel), (int) HttpStatusCode.OK)]
        public Task<IActionResult> PostFxPaygate([FromBody] PaymentUrlRequestModel input)
        {
            return GetPaymentUrl(input);
        }

        /// <summary>
        /// Get Url for PaymentSystem
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns with Url for PaymentSystem</returns>
        [HttpPost]
        [Route("paymentUrl")]
        [SwaggerOperation("GetPaymentUrl")]
        [ProducesResponseType(typeof(PaymentUrlResponseModel), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetPaymentUrl([FromBody] PaymentUrlRequestModel input)
        {
            var clientInfoTask = _clientAccountClient.ClientAccountInformation.GetByIdAsync(_requestContext.ClientId);
            var pdTask = _personalDataService.GetAsync(_requestContext.ClientId);

            await Task.WhenAll(clientInfoTask, pdTask);

            var pd = pdTask.Result;

            var result = await _link4PayServiceClient.GetPaymentUrlAsync(new PaymentUrlRequest
            {
                Transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Amount = input.Amount,
                    AssetId = input.AssetId,
                    ClientId = _requestContext.ClientId,
                    ExternalClientId = clientInfoTask.Result.ExternalId
                },
                Details = new DetailsInfo
                {
                    FirstName = pd.FirstName,
                    LastName = pd.LastName,
                    Email = pd.Email,
                    Phone = pd.ContactPhone,
                    CountryIso3 = pd.CountryFromPOA
                },
                Urls = new UrlsInfo
                {
                    OkUrl = input.OkUrl,
                    CancelUrl = input.CancelUrl,
                    FailUrl = input.FailUrl
                }
            });

            if (result.Error != null)
            {
                if (result.Error.ErrorType == ErrorDetails.Types.ErrorType.CurrencyNotSupported)
                {
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);
                }

                if (result.Error.ErrorType == ErrorDetails.Types.ErrorType.CountryNotSupported)
                {
                    throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.CountryUnavailable);
                }
            }

            var resp = new PaymentUrlResponseModel
            {
                Url = result.PaymentUrl,
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

            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(_requestContext.ClientId);

            if (isKycNeeded)
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

            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(_requestContext.ClientId);

            if (isKycNeeded)
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

            var isKycNeded = await _kycStatusService.IsKycNeededAsync(_requestContext.ClientId);

            if (isKycNeded)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);

            if (asset.BlockchainIntegrationType == BlockchainIntegrationType.Sirius)
            {
                await _siriusWalletsService.CreateWalletsAsync(_requestContext.ClientId);
                return Ok();
            }

            var isFirstGeneration = string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId);

            try
            {
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
            }
            catch (DuplicationWalletException)
            {
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressAlreadyGenerated);
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

            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(_requestContext.ClientId);

            if (isKycNeeded)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.KycRequired);

            if (asset.BlockchainIntegrationType == BlockchainIntegrationType.Sirius)
            {
                var address = await _siriusWalletsService.GetWalletAdderssAsync(_requestContext.ClientId, asset.SiriusAssetId);

                if (address == null)
                    throw LykkeApiErrorException.BadRequest(
                        LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressNotGenerated);

                return Ok(new CryptoDepositAddressRespModel
                {
                    Address = string.IsNullOrEmpty(address.Tag) ? address.Address : $"{address.Address}+{address.Tag}",
                    BaseAddress = address.Address,
                    AddressExtension = address.Tag
                });
            }
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
