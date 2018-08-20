using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Settings;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts.Cashout;
using Lykke.Service.Operations.Contracts.Commands;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IBalancesClient _balancesClient;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly FeeSettings _feeSettings;
        private readonly BaseSettings _baseSettings;
        private readonly IOperationsClient _operationsClient;
        private readonly IRequestContext _requestContext;

        public OperationsController(
            IAssetsServiceWithCache assetsServiceWithCache,
            IBalancesClient balancesClient,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            FeeSettings feeSettings,
            BaseSettings baseSettings,
            IOperationsClient operationsClient,            
            IRequestContext requestContext)
        {
            _assetsServiceWithCache = assetsServiceWithCache;
            _balancesClient = balancesClient;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _feeSettings = feeSettings;
            _baseSettings = baseSettings;
            _operationsClient = operationsClient;
            _requestContext = requestContext;
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
        [Route("transfer/{id}")]
        public async Task<IActionResult> Transfer([FromBody]CreateTransferRequest cmd, Guid id)
        {
            await _operationsClient.Transfer(id, 
                new CreateTransferCommand
                {
                    ClientId = new Guid(_requestContext.ClientId),
                    Amount = cmd.Amount,
                    SourceWalletId = 
                    cmd.SourceWalletId,
                    WalletId = cmd.WalletId,
                    AssetId = cmd.AssetId                    
                });
            
            return Created(Url.Action("Get", new { id }), id);
        }

        [HttpPost]
        [Route("cashout/{id}")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Cashout([FromBody] CreateCashoutRequest cmd, Guid id)
        {
            if (string.IsNullOrWhiteSpace(cmd.DestinationAddress) || string.IsNullOrWhiteSpace(cmd.AssetId) || cmd.Volume == 0m)
                throw new ClientException(HttpStatusCode.BadRequest, ExceptionType.InvalidInput);

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(cmd.AssetId);

            if (asset == null)
            {
                return NotFound($"Asset '{cmd.AssetId}' not found.");
            }

            var balance = await _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(cmd.AssetId, _requestContext.ClientId));
            var cashoutSettings = await _clientAccountClient.GetCashOutBlockAsync(_requestContext.ClientId);
            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            var cashoutCommand = new CreateCashoutCommand
            {
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
                    CashoutMinimalAmount = (decimal) asset.CashoutMinimalAmount,
                    LowVolumeAmount = (decimal?) asset.LowVolumeAmount ?? 0,
                    LykkeEntityId = asset.LykkeEntityId
                },
                Client = new ClientCashoutModel
                {
                    Id = new Guid(_requestContext.ClientId),                    
                    Balance = balance?.Balance ?? 0,
                    CashOutBlocked = cashoutSettings.CashOutBlocked,
                    KycStatus = kycStatus.ToString()
                },
                GlobalSettings = new GlobalSettingsCashoutModel
                {
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

            try
            {
                await _operationsClient.CreateCashout(id, cashoutCommand);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.BadRequest)
                    return BadRequest(JObject.Parse(e.Response.Content));

                throw;
            }

            return Created(Url.Action("Get", new { id }), id);
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