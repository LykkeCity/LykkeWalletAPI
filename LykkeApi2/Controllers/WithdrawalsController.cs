using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core.Constants;
using Core.Services;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Contract.Requests;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.PersonalData.Contract;
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
        private readonly ILog _log;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;
        private readonly IBlockchainCashoutPreconditionsCheckClient _blockchainCashoutPreconditionsCheckClient;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly BlockedWithdawalSettings _blockedWithdawalSettings;
        private readonly IRequestContext _requestContext;

        public WithdrawalsController(
            ILog log,
            IAssetsHelper assetsHelper,
            IBlockchainWalletsClient blockchainWalletsClient,
            IBlockchainCashoutPreconditionsCheckClient blockchainCashoutPreconditionsCheckClient,
            IFeeCalculatorClient feeCalculatorClient,
            IPersonalDataService personalDataService,
            BlockedWithdawalSettings blockedWithdawalSettings,
            IRequestContext requestContext)
        {
            _log = log;
            _assetsHelper = assetsHelper;
            _blockchainWalletsClient = blockchainWalletsClient;
            _blockchainCashoutPreconditionsCheckClient = blockchainCashoutPreconditionsCheckClient;
            _feeCalculatorClient = feeCalculatorClient;
            _personalDataService = personalDataService;
            _blockedWithdawalSettings = blockedWithdawalSettings;
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
        /// Get fees for crypto withdrawal
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("crypto/fees")]
        [ProducesResponseType(typeof(List<WithdrawalCashoutFeeModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCryptoFees()
        {
            var assetsTask = _assetsHelper.GetAllAssetsAsync();
            var feesTask = _feeCalculatorClient.GetCashoutFeesAsync();

            await Task.WhenAll(assetsTask, feesTask);

            var assets = assetsTask.Result
                .Where(x => !x.IsDisabled)
                .ToList();

            var cashoutFees = feesTask.Result;

            var result = new List<WithdrawalCashoutFeeModel>();

            foreach (var fee in cashoutFees)
            {
                var asset = assets.FirstOrDefault(x => x.Id == fee.AssetId);

                if (asset == null)
                    continue;

                result.Add(new WithdrawalCashoutFeeModel
                {
                    AssetId = asset.Id,
                    AssetDisplayId = asset.DisplayId,
                    FeeSize = ((decimal)fee.Size).ToString(CultureInfo.InvariantCulture),
                    FeeType = fee.Type,
                    MinCashoutAmount = ((decimal)asset.CashoutMinimalAmount).ToString(CultureInfo.InvariantCulture),
                });
            }

            return Ok(result);
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

            var blockchainCapabilities = await _blockchainWalletsClient.GetCapabilititesAsync(asset.BlockchainIntegrationLayerId);

            string destinationAddress;

            if (blockchainCapabilities.IsPublicAddressExtensionRequired)
            {
                try
                {
                    destinationAddress = await _blockchainWalletsClient.MergeAddressAsync(
                        asset.BlockchainIntegrationLayerId,
                        baseAddress,
                        addressExtension);
                }
                catch (Exception e)
                {
                    _log.WriteWarning(nameof(WithdrawalsController), nameof(ValidateCryptoAddress), e.Message);
                    return Ok(
                        new WithdrawalCryptoAddressValidationModel
                        {
                            IsValid = false
                        });
                }
            }
            else
            {
                destinationAddress = baseAddress;
            }

            var clientId = _requestContext.ClientId;

            var addressValidationResult = await _blockchainCashoutPreconditionsCheckClient.ValidateCashoutAsync(
                new CheckCashoutValidityModel()
                {
                    AssetId = assetId,
                    DestinationAddress = destinationAddress,
                    ClientId = Guid.Parse(clientId),
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
            var assetsTask = _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);
            var pdTask = _personalDataService.GetAsync(_requestContext.ClientId);

            await Task.WhenAll(assetsTask, pdTask);

            var assets = assetsTask.Result.ToList();
            var pd = pdTask.Result;

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
                    .Where(x => x.SwiftWithdrawal && (!_blockedWithdawalSettings.AssetByCountry.ContainsKey(x.Id) ||
                                                      !_blockedWithdawalSettings.AssetByCountry[x.Id].Contains(pd.CountryFromPOA, StringComparer.InvariantCultureIgnoreCase)))
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
        [ProducesResponseType(typeof(SwiftLastResponse), (int)HttpStatusCode.OK)]
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
