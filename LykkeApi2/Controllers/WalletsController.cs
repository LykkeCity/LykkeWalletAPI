using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.ClientAccount.Client.Models.Response.Wallets;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.HftInternalService.Client.Keys;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ApiKey;
using LykkeApi2.Models.ClientBalancesModels;
using LykkeApi2.Models.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ClientBalanceResponseModel = LykkeApi2.Models.ClientBalancesModels.ClientBalanceResponseModel;
using CreateWalletRequest = LykkeApi2.Models.Wallets.CreateWalletRequest;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/wallets")]
    [ApiController]
    public class WalletsController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IBalancesClient _balancesClient;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IHftInternalClient _hftInternalService;
        private readonly IAssetsHelper _assetsHelper;

        public WalletsController(
            IRequestContext requestContext,
            IClientAccountClient clientAccountService,
            IBalancesClient balancesClient,
            IHftInternalClient hftInternalService,
            IAssetsHelper assetsHelper)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
            _balancesClient = balancesClient ?? throw new ArgumentNullException(nameof(balancesClient));
            _hftInternalService = hftInternalService ?? throw new ArgumentNullException(nameof(hftInternalService));
            _assetsHelper = assetsHelper ?? throw new ArgumentNullException(nameof(assetsHelper));
        }

        /// <summary>
        /// Create wallet.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        [SwaggerOperation("CreateWallet")]
        public async Task<WalletModel> CreateWallet([FromBody] CreateWalletRequest request)
        {
            var wallet = await _clientAccountService.Wallets.CreateWalletAsync(
                new Lykke.Service.ClientAccount.Client.Models.Request.Wallets.CreateWalletRequest
                {
                    ClientId = _requestContext.ClientId,
                    Description = request.Description,
                    Name = request.Name,
                    Type = request.Type,
                    Owner = OwnerType.Spot
                }
            );

            return new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type.ToString(),
                Description = wallet.Description
            };
        }

        /// <summary>
        /// Create trusted wallet and generate API-key.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("hft")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [SwaggerOperation("CreateApiWallet")]
        public async Task<IActionResult> CreateApiWallet([FromBody] CreateApiKeyRequest request)
        {
            var apiKey = await _hftInternalService.Keys.CreateKey(new CreateApiKeyModel
            {

                ClientId = _requestContext.ClientId,
                Name = request.Name,
                Description = request.Description,
                Apiv2Only = request.Apiv2Only
            });

            if (apiKey == null)
                return BadRequest();

            return Ok(new CreateApiKeyResponse { ApiKey = apiKey.ApiKey, WalletId = apiKey.WalletId, Name = request.Name, Description = request.Description, Apiv2Only = request.Apiv2Only});
        }

        /// <summary>
        /// Modify existing wallet.
        /// </summary>
        /// <param name="id">Wallet id.</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPut("{id}")]
        [SwaggerOperation("ModifyWallet")]
        public async Task<IActionResult> ModifyWallet(string id, [FromBody]ModifyWalletRequest request)
        {
            // checking if wallet exists and user owns the specified wallet
            var wallet = await GetClientWallet(id);
            if (wallet == null)
                return NotFound();

            wallet = await _clientAccountService.Wallets.ModifyWalletAsync(id,
                new Lykke.Service.ClientAccount.Client.Models.Request.Wallets.ModifyWalletRequest
                {
                    Name = request.Name, Description = request.Description
                });

            if (wallet == null)
                return NotFound();

            return Ok(new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type.ToString(), Description = wallet.Description });
        }

        /// <summary>
        /// Delete wallet.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [SwaggerOperation("DeleteWallet")]
        public async Task<IActionResult> DeleteWallet(string id)
        {
            // checking if wallet exists and user owns the specified wallet
            var wallet = await GetClientWallet(id);

            if (wallet == null)
                return NotFound();

            await _clientAccountService.Wallets.DeleteWalletAsync(id);

            return Ok();
        }

        /// <summary>
        /// Get all client wallets.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WalletModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [SwaggerOperation("GetWallets")]
        public async Task<IActionResult> GetWallets()
        {
            var wallets = await _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId, owner: OwnerType.Spot);

            if (wallets == null)
                return NotFound();

            var clientKeys = await _hftInternalService.Keys.GetKeys(_requestContext.ClientId);
            return Ok(wallets.Select(wallet => new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type.ToString(),
                Description = wallet.Description,
                ApiKey = clientKeys.FirstOrDefault(x => x.WalletId == wallet.Id)?.ApiKey,
                Apiv2Only = clientKeys.FirstOrDefault(x => x.WalletId == wallet.Id)?.Apiv2Only ?? false
            }));
        }

        /// <summary>
        /// Get specified wallet.
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation("GetWallet")]
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetWallet(string id)
        {
            // checking if wallet exists and user owns the specified wallet
            var wallet = await GetClientWallet(id);
            if (wallet == null)
                return NotFound();

            var clientKeys = await _hftInternalService.Keys.GetKeys(_requestContext.ClientId);
            return Ok(new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type.ToString(),
                Description = wallet.Description,
                ApiKey = clientKeys.FirstOrDefault(x => x.WalletId == wallet.Id)?.ApiKey
            });
        }

        /// <summary>
        /// Get all wallets balances.
        /// </summary>
        [Authorize]
        [HttpGet("balances")]
        [SwaggerOperation("GetBalances")]
        [ProducesResponseType(typeof(IEnumerable<WalletBalancesModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetBalances()
        {
            var result = new List<WalletBalancesModel>();

            var walletsTask = _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId, owner: OwnerType.Spot);
            var clientKeysTask = _hftInternalService.Keys.GetKeys(_requestContext.ClientId);
            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            await Task.WhenAll(walletsTask, clientKeysTask, availableAssetsTask);

            var wallets = walletsTask.Result;
            var clientKeys = clientKeysTask.Result;
            var availableAssets = availableAssetsTask.Result;

            foreach (var wallet in wallets)
            {
                var walletBalances = await _balancesClient.GetClientBalances(wallet.Type == WalletType.Trading ? _requestContext.ClientId : wallet.Id);
                var balancesToShow = walletBalances?
                    .Where(x => availableAssets.Contains(x.AssetId) && x.Balance > 0)
                    .Select(ClientBalanceResponseModel.Create);
                result.Add(new WalletBalancesModel
                {
                    Id = wallet.Id,
                    Type = wallet.Type.ToString(),
                    Name = wallet.Name,
                    Description = wallet.Description,
                    Balances = balancesToShow ?? new ClientBalanceResponseModel[0],
                    ApiKey = clientKeys.FirstOrDefault(x => x.WalletId == wallet.Id)?.ApiKey
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get trading wallet balances.
        /// </summary>
        [Authorize]
        [HttpGet("trading/balances")]
        [SwaggerOperation("GetTradingWalletBalances")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTradingWalletBalances()
        {
            var clientBalancesTask = _balancesClient.GetClientBalances(_requestContext.ClientId);
            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            await Task.WhenAll(clientBalancesTask, availableAssetsTask);

            var balancesToShow = clientBalancesTask.Result?
                .Where(x => availableAssetsTask.Result.Contains(x.AssetId) && x.Balance > 0)
                .Select(ClientBalanceResponseModel.Create);

            return Ok(balancesToShow ?? new ClientBalanceResponseModel[0]);
        }

        /// <summary>
        /// Get specified wallet balances.
        /// </summary>
        [Authorize]
        [HttpGet("{walletId}/balances")]
        [SwaggerOperation("GetWalletBalances")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetWalletBalances(string walletId)
        {
            // checking if wallet exists and user owns the specified wallet
            var wallet = await GetClientWallet(walletId);
            if (wallet == null)
                return NotFound();

            var clientBalancesTask = _balancesClient.GetClientBalances(walletId);
            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            await Task.WhenAll(clientBalancesTask, availableAssetsTask);

            var balancesToShow = clientBalancesTask.Result?
                .Where(x => availableAssetsTask.Result.Contains(x.AssetId) && x.Balance > 0)
                .Select(ClientBalanceResponseModel.Create);

            return Ok(balancesToShow ?? new ClientBalanceResponseModel[0]);
        }

        /// <summary>
        /// Get balances by asset id.
        /// </summary>
        [Authorize]
        [HttpGet("balances/{assetId}")]
        [SwaggerOperation("GetBalancesByAssetId")]
        [ProducesResponseType(typeof(IEnumerable<WalletAssetBalanceModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetBalancesByAssetId(string assetId)
        {
            var result = new List<WalletAssetBalanceModel>();

            var walletsTask = _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId, owner: OwnerType.Spot);
            var clientKeysTask = _hftInternalService.Keys.GetKeys(_requestContext.ClientId);
            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);
            await Task.WhenAll(walletsTask, clientKeysTask);

            var wallets = walletsTask.Result;
            var clientKeys = clientKeysTask.Result;
            var availableAssets = availableAssetsTask.Result;

            if (!availableAssets.Contains(assetId))
                return NotFound();

            foreach (var wallet in wallets)
            {
                var balance = await _balancesClient.GetClientBalanceByAssetId(
                    new ClientBalanceByAssetIdModel
                    {
                        ClientId = wallet.Type == WalletType.Trading ? _requestContext.ClientId : wallet.Id,
                        AssetId = assetId
                    });

                result.Add(new WalletAssetBalanceModel
                {
                    Id = wallet.Id,
                    Type = wallet.Type.ToString(),
                    Name = wallet.Name,
                    Description = wallet.Description,
                    Balances = balance != null ? ClientBalanceResponseModel.Create(balance) : new ClientBalanceResponseModel { AssetId = assetId, Balance = 0, Reserved = 0},
                    ApiKey = clientKeys.FirstOrDefault(x => x.WalletId == wallet.Id)?.ApiKey
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get trading wallet balances by asset id.
        /// </summary>
        [Authorize]
        [HttpGet("trading/balances/{assetId}")]
        [SwaggerOperation("GetTradindWalletBalanceByAssetId")]
        [ProducesResponseType(typeof(ClientBalanceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetTradindWalletBalanceByAssetId(string assetId)
        {
            var clientBalanceResultTask = _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = _requestContext.ClientId,
                    AssetId = assetId
                });

            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            await Task.WhenAll(clientBalanceResultTask, availableAssetsTask);

            if (!availableAssetsTask.Result.Contains(assetId))
                return NotFound();

            if (clientBalanceResultTask.Result != null && string.IsNullOrEmpty(clientBalanceResultTask.Result.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResultTask.Result));
            }

            return NotFound();
        }

        /// <summary>
        /// Get specified wallet balances by asset id.
        /// </summary>
        [Authorize]
        [HttpGet("{walletId}/balances/{assetId}")]
        [SwaggerOperation("GetWalletBalanceByAssetId")]
        [ProducesResponseType(typeof(ClientBalanceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetWalletBalanceByAssetId(string walletId, string assetId)
        {
            // checking if wallet exists and user owns the specified wallet
            var wallet = await GetClientWallet(walletId);
            if (wallet == null)
                return NotFound();

            var clientBalanceResultTask = _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = walletId,
                    AssetId = assetId
                });

            var availableAssetsTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId);

            await Task.WhenAll(clientBalanceResultTask, availableAssetsTask);

            if (!availableAssetsTask.Result.Contains(assetId))
                return NotFound();

            if (clientBalanceResultTask.Result != null && string.IsNullOrEmpty(clientBalanceResultTask.Result.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResultTask.Result));
            }

            return NotFound();
        }

        private async Task<WalletInfo> GetClientWallet(string walletId)
        {
            var wallets = await _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId);
            return wallets?.FirstOrDefault(x => x.Id == walletId);
        }
    }
}
