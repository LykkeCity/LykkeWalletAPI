using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ConfirmationCodes.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Contracts.Cashout;
using Lykke.Service.Operations.Contracts.SwiftCashout;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models._2Fa;
using LykkeApi2.Models.Operations;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Refit;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;

namespace LykkeApi2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/operations")]
    [ApiController]
    public class OperationsController : Controller
    {
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IBalancesClient _balancesClient;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly FeeSettings _feeSettings;
        private readonly BaseSettings _baseSettings;
        private readonly IOperationsClient _operationsClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IRequestContext _requestContext;
        private readonly IConfirmationCodesClient _confirmationCodesClient;
        private readonly Google2FaService _google2FaService;

        public OperationsController(
            IAssetsServiceWithCache assetsServiceWithCache,
            IBalancesClient balancesClient,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            FeeSettings feeSettings,
            BaseSettings baseSettings,
            IOperationsClient operationsClient,
            ICqrsEngine cqrsEngine,
            IRequestContext requestContext,
            IConfirmationCodesClient confirmationCodesClient,
            Google2FaService google2FaService)
        {
            _assetsServiceWithCache = assetsServiceWithCache;
            _balancesClient = balancesClient;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _feeSettings = feeSettings;
            _baseSettings = baseSettings;
            _operationsClient = operationsClient;
            _cqrsEngine = cqrsEngine;
            _requestContext = requestContext;
            _confirmationCodesClient = confirmationCodesClient;
            _google2FaService = google2FaService;
        }

        /// <summary>
        /// Get operation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            OperationModel operation = null;

            try
            {
                operation = await _operationsClient.Get(id);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                throw;
            }

            if (operation == null)
                return NotFound();

            return Ok(operation.ToApiModel());
        }

        /// <summary>
        /// Create transfer operation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("transfer")]
        [ProducesResponseType(typeof(Google2FaResultModel<string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Transfer([FromBody]CreateTransferRequest cmd, [FromQuery] Guid? id)
        {
            var check2FaResult = await _google2FaService.Check2FaAsync<string>(_requestContext.ClientId, cmd.Code2Fa);

            if (check2FaResult != null)
                return Ok(check2FaResult);

            var operationId = id ?? Guid.NewGuid();

            await _operationsClient.Transfer(operationId,
                new CreateTransferCommand
                {
                    ClientId = new Guid(_requestContext.ClientId),
                    Amount = cmd.Amount,
                    SourceWalletId =
                        cmd.SourceWalletId,
                    WalletId = cmd.WalletId,
                    AssetId = cmd.AssetId
                });

            return Ok(Google2FaResultModel<string>.Success(id.ToString()));
        }

        /// <summary>
        /// Create cashout operation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("cashout/crypto")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Cashout([FromBody] CreateCashoutRequest cmd, [FromQuery]Guid? id)
        {
            if (string.IsNullOrWhiteSpace(cmd.DestinationAddress) || string.IsNullOrWhiteSpace(cmd.AssetId) || cmd.Volume == 0m)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(cmd.AssetId);

            if (asset == null)
            {
                return NotFound($"Asset '{cmd.AssetId}' not found.");
            }

            var balance = await _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(cmd.AssetId, _requestContext.ClientId));
            var cashoutSettings = await _clientAccountClient.ClientSettings.GetCashOutBlockSettingsAsync(_requestContext.ClientId);
            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            if (_baseSettings.EnableTwoFactor)
            {
                try
                {
                    if ((await _confirmationCodesClient.Google2FaIsClientBlacklistedAsync(_requestContext.ClientId)).IsClientBlacklisted)
                        throw LykkeApiErrorException.Forbidden(LykkeApiErrorCodes.Service.SecondFactorCheckForbiden);
                }
                catch (ApiException e)
                {
                    if (e.StatusCode == HttpStatusCode.BadRequest)
                        throw LykkeApiErrorException.Forbidden(LykkeApiErrorCodes.Service.TwoFactorRequired);
                }
            }

            var operationId = id ?? Guid.NewGuid();

            var cashoutCommand = new CreateCashoutCommand
            {
                OperationId = operationId,
                DestinationAddress = cmd.DestinationAddress,
                DestinationAddressExtension = cmd.DestinationAddressExtension,
                Volume = cmd.Volume,
                Asset = new AssetCashoutModel
                {
                    Id = asset.Id,
                    DisplayId = asset.DisplayId,
                    MultiplierPower = asset.MultiplierPower,
                    AssetAddress = asset.AssetAddress,
                    Accuracy = asset.Accuracy,
                    BlockchainIntegrationLayerId = asset.BlockchainIntegrationLayerId,
                    Blockchain = asset.Blockchain.ToString(),
                    Type = asset.Type?.ToString(),
                    IsTradable = asset.IsTradable,
                    IsTrusted = asset.IsTrusted,
                    KycNeeded = asset.KycNeeded,
                    BlockchainWithdrawal = asset.BlockchainWithdrawal,
                    CashoutMinimalAmount = (decimal)asset.CashoutMinimalAmount,
                    LowVolumeAmount = (decimal?)asset.LowVolumeAmount ?? 0,
                    LykkeEntityId = asset.LykkeEntityId,
                    SiriusAssetId = asset.SiriusAssetId,
                    BlockchainIntegrationType = asset.BlockchainIntegrationType
                },
                Client = new ClientCashoutModel
                {
                    Id = new Guid(_requestContext.ClientId),
                    Balance = balance?.Balance ?? 0,
                    CashOutBlocked = cashoutSettings.CashOutBlocked,
                    KycStatus = kycStatus.ToString(),
                    ConfirmationType = "google"
                },
                GlobalSettings = new GlobalSettingsCashoutModel
                {
                    MaxConfirmationAttempts = _baseSettings.MaxTwoFactorConfirmationAttempts,
                    TwoFactorEnabled = _baseSettings.EnableTwoFactor,
                    CashOutBlocked = false, // TODO
                    FeeSettings = new FeeSettingsCashoutModel
                    {
                        TargetClients = new Dictionary<string, string>
                        {
                            { "Cashout", _feeSettings.TargetClientId.Cashout }
                        }
                    }
                }
            };

            _cqrsEngine.SendCommand(cashoutCommand, "apiv2", OperationsBoundedContext.Name);

            return Created(Url.Action("Get", new { operationId }), operationId);
        }

        /// <summary>
        /// Create swift cashout operation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("cashout/swift")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> SwiftCashout([FromBody] CreateSwiftCashoutRequest cmd, [FromQuery] Guid? id)
        {
            if (string.IsNullOrWhiteSpace(cmd.AssetId) || cmd.Volume == 0m)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(cmd.AssetId);

            if (asset == null)
            {
                return NotFound($"Asset '{cmd.AssetId}' not found.");
            }

            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            var operationId = id ?? Guid.NewGuid();

            var command = new CreateSwiftCashoutCommand
            {
                OperationId = operationId,
                Volume = cmd.Volume,
                Asset = new SwiftCashoutAssetModel
                {
                    Id = asset.Id,
                    KycNeeded = asset.KycNeeded,
                    SwiftCashoutEnabled = asset.SwiftWithdrawal,
                    LykkeEntityId = asset.LykkeEntityId
                },
                Client = new SwiftCashoutClientModel
                {
                    Id = new Guid(_requestContext.ClientId),
                    KycStatus = kycStatus.ToString()
                },
                Swift = new SwiftFieldsModel
                {
                    AccHolderAddress = cmd.AccHolderAddress,
                    AccHolderCity = cmd.AccHolderCity,
                    AccHolderZipCode = cmd.AccHolderZipCode,
                    AccName = cmd.AccName,
                    AccNumber = cmd.AccNumber,
                    BankName = cmd.BankName,
                    Bic = cmd.Bic
                },
                CashoutSettings = new SwiftCashoutSettingsModel
                {
                    FeeTargetId = _feeSettings.TargetClientId.Withdrawal,
                    HotwalletTargetId = _baseSettings.CashoutSettings.SwiftHotwallet
                }
            };

            _cqrsEngine.SendCommand(command, "apiv2", OperationsBoundedContext.Name);

            return Created(Url.Action("Get", new { operationId }), operationId);
        }

        /// <summary>
        /// Cancel operation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("cancel/{id}")]
        public async Task Cancel(Guid id)
        {
            await _operationsClient.Cancel(id);
        }
    }
}
