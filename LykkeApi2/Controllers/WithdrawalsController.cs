using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Core.Exceptions;
using Core.Services;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.FeeCalculator.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Withdrawals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/withdrawals")]
    public class WithdrawalsController : Controller
    {
        private readonly IAssetsHelper _assetsHelper;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;
        private readonly IBlockchainCashoutPreconditionsCheckClient _blockchainCashoutPreconditionsCheckClient;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IRequestContext _requestContext;

        public WithdrawalsController(
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IBlockchainCashoutPreconditionsCheckClient blockchainCashoutPreconditionsCheckClient,
            IFeeCalculatorClient feeCalculatorClient,
            IRequestContext requestContext)
        {
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _blockchainCashoutPreconditionsCheckClient = blockchainCashoutPreconditionsCheckClient;
            _feeCalculatorClient = feeCalculatorClient;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get additional asset info for crypto withdrawal
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("crypto/{assetId}/info")]
        [ProducesResponseType(typeof(WithdrawalCryptoInfoModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetInfo([FromRoute] string assetId)
        {
            var asset = await GetCryptoWithdrawalAssetAsync(assetId);

            if (string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId))
            {
                return Ok(new WithdrawalCryptoInfoModel());
            }

            var blockchainCapabilities = await _blockchainWalletsClient.GetCapabilititesAsync(asset.BlockchainIntegrationLayerId);

            var constants =
                blockchainCapabilities.IsPublicAddressExtensionRequired
                    ? await _blockchainWalletsClient.GetAddressExtensionConstantsAsync(
                        asset.BlockchainIntegrationLayerId)
                    : null;

            return Ok(new WithdrawalCryptoInfoModel
            {
                AddressExtensionMandatory = blockchainCapabilities.IsPublicAddressExtensionRequired,
                BaseAddressTitle = constants?.BaseAddressDisplayName,
                AddressExtensionTitle = constants?.AddressExtensionDisplayName
            });
        }

        /// <summary>
        /// Get asset fee for crypto withdrawal
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("crypto/{assetId}/fee")]
        [ProducesResponseType(typeof(WithdrawalCryptoFeeModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCryptoFee([FromRoute] string assetId)
        {
            var asset = await GetCryptoWithdrawalAssetAsync(assetId);

            var fee = await _feeCalculatorClient.GetCashoutFeeAsync(asset.Id);

            return Ok(fee.ToApiModel());
        }

        /// <summary>
        /// Validate asset withdrawal address and extension (if presented)
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="baseAddress"></param>
        /// <param name="addressExtension"></param>
        /// <returns></returns>
        [HttpGet("crypto/{assetId}/validateAddress")]
        [ProducesResponseType(typeof(WithdrawalCryptoAddressValidationModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateCryptoAddress(
            [FromRoute] string assetId,
            [FromQuery] string baseAddress,
            [FromQuery] string addressExtension)
        {
            var asset = await GetCryptoWithdrawalAssetAsync(assetId);

            if (string.IsNullOrWhiteSpace(baseAddress))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            if (string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId))
            {
                return Ok(new WithdrawalCryptoAddressValidationModel
                {
                    IsValid = true
                });
            }

            var blockchainCapabilities =
                await _blockchainWalletsClient.GetCapabilititesAsync(asset.BlockchainIntegrationLayerId);

            if (blockchainCapabilities.IsPublicAddressExtensionRequired &&
                string.IsNullOrWhiteSpace(addressExtension))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var destinationAddress = blockchainCapabilities.IsPublicAddressExtensionRequired
                ? await _blockchainWalletsClient.MergeAddressAsync(
                    asset.BlockchainIntegrationLayerId,
                    baseAddress,
                    addressExtension)
                : baseAddress;

            var addressValidationResult = await _blockchainCashoutPreconditionsCheckClient.ValidateCashoutAsync(
                new CashoutValidateModel
                {
                    AssetId = assetId,
                    DestinationAddress = destinationAddress
                });

            return Ok(
                new WithdrawalCryptoAddressValidationModel
                {
                    IsValid = addressValidationResult.isAllowed
                });
        }

        /// <summary>
        /// Get available assets for crypto and swift withdrawal
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        [ProducesResponseType(typeof(WithdrawalMethodsResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableMethods()
        {
            var assets = (await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId)).ToList();

            var cryptos = new WithdrawalMethod
            {
                Name = "Cryptos",
                Assets = assets
                    .Where(x => x.BlockchainWithdrawal)
                    .Select(x => x.Id)
                    .ToList()
            };

            var swift = new WithdrawalMethod
            {
                Name = "Swift",
                Assets = assets
                    .Where(x => x.SwiftWithdrawal)
                    .Select(x => x.Id)
                    .ToList()
            };

            var model = new WithdrawalMethodsResponse
            {
                WithdrawalMethods = new List<WithdrawalMethod>
                {
                    cryptos,
                    swift
                }
            };

            return Ok(model);
        }

        /// <summary>
        /// Get last swift cashout data
        /// </summary>
        /// <returns></returns>
        [HttpGet("swift/last")]
        [ProducesResponseType(typeof(SwiftLastResponse), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetLastSwift()
        {
            return Ok(new SwiftLastResponse());
        }

        private async Task<Asset> GetCryptoWithdrawalAssetAsync(string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);

            if (asset == null || asset.IsDisabled)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);

            if (!asset.BlockchainWithdrawal)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            return asset;
        }
    }
}