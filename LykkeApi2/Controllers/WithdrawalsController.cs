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

        [HttpGet("crypto/{assetId}/info")]
        [ProducesResponseType(typeof(WithdrawalCryptoInfoModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetInfo([FromRoute] string assetId)
        {
            var asset = await GetAssetAsync(assetId);

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

        [HttpGet("crypto/{assetId}/fee")]
        [ProducesResponseType(typeof(WithdrawalCryptoFeeModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCryptoFee([FromRoute] string assetId)
        {
            var asset = await GetAssetAsync(assetId);

            var fee = await _feeCalculatorClient.GetCashoutFeeAsync(assetId);

            return Ok(fee.ToApiModel());
        }

        [HttpGet("crypto/{assetId}/validateAddress")]
        [ProducesResponseType(typeof(WithdrawalCryptoAddressValidationModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ValidateCryptoAddress(
            [FromRoute] string assetId,
            [FromQuery] string baseAddress,
            [FromQuery] string addressExtension)
        {
            var asset = await GetAssetAsync(assetId);

            if (string.IsNullOrWhiteSpace(baseAddress))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var blockchainCapabilities = await _blockchainWalletsClient.GetCapabilititesAsync(asset.BlockchainIntegrationLayerId);

            if (blockchainCapabilities.IsPublicAddressExtensionRequired && string.IsNullOrWhiteSpace(addressExtension))
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

        private async Task<Asset> GetAssetAsync(string assetId)
        {
            var asset = await _assetsHelper.GetAssetAsync(assetId);

            if (asset == null || asset.IsDisabled)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetNotFound);

            if (asset.BlockchainWithdrawal == false)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            return asset;
        }
    }
}