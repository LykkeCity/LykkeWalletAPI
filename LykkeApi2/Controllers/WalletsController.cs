using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ApiKey;
using LykkeApi2.Models.ClientBalancesModels;
using LykkeApi2.Models.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using ClientBalanceResponseModel = LykkeApi2.Models.ClientBalancesModels.ClientBalanceResponseModel;
using CreateWalletRequest = LykkeApi2.Models.Wallets.CreateWalletRequest;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/wallets")]
    public class WalletsController : Controller
    {
        private string TradingWalletType => Lykke.Service.ClientAccount.Client.AutorestClient.Models.WalletType.Trading.ToString();

        private readonly IRequestContext _requestContext;
        private readonly IBalancesClient _balancesClient;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IHftInternalServiceAPI _hftInternalService;

        public WalletsController(IRequestContext requestContext, IClientAccountClient clientAccountClient, IBalancesClient balancesClient, IHftInternalServiceAPI hftInternalService)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountClient = clientAccountClient ?? throw new ArgumentNullException(nameof(clientAccountClient));
            _balancesClient = balancesClient ?? throw new ArgumentNullException(nameof(balancesClient));
            _hftInternalService = hftInternalService ?? throw new ArgumentNullException(nameof(hftInternalService));
        }

        /// <summary>
        /// Create wallet.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        [SwaggerOperation("CreateWallet")]
        public async Task<WalletModel> CreateWallet([FromBody] CreateWalletRequest request)
        {
            var wallet = await _clientAccountClient.CreateWalletAsync(_requestContext.ClientId, request.Type, request.Name, request.Description);

            return new WalletModel {Id = wallet.Id, Name = wallet.Name, Type = wallet.Type, Description = wallet.Description};
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
            var apiKey = await _hftInternalService.CreateKeyAsync(
                new Lykke.Service.HftInternalService.Client.AutorestClient.Models.CreateApiKeyRequest(_requestContext.ClientId, request.Name, request.Description));

            if (apiKey == null)
                return BadRequest();

            return Ok(new CreateApiKeyResponse { ApiKey = apiKey.Key, WalletId = apiKey.Wallet });
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
            // checking if user owns the specified wallet
            var wallets = await _clientAccountClient.GetWalletsByClientIdAsync(_requestContext.ClientId);
            var wallet = wallets?.FirstOrDefault(x => x.Id == id);
            if (wallet == null)
                return NotFound();

            wallet = await _clientAccountClient.ModifyWalletAsync(id, request.Name, request.Description);
            if (wallet == null)
                return NotFound();

            return Ok(new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type, Description = wallet.Description });
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
            // checking if user owns the specified wallet

            var wallets = await _clientAccountClient.GetWalletsByClientIdAsync(_requestContext.ClientId);
            var wallet = wallets?.FirstOrDefault(x => x.Id == id);
            if (wallet == null)
                return NotFound();

            // todo: always delete wallet through ClientAccountService; HFT internal service should process deleted messages by itself
            var apiKey = (await _hftInternalService.GetKeysAsync(_requestContext.ClientId))?.FirstOrDefault(x => x.Wallet == id);
            if (apiKey != null)
            {
                await _hftInternalService.DeleteKeyAsync(apiKey.Key);
            }
            await _clientAccountClient.DeleteWalletAsync(id);
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
            var wallets = await _clientAccountClient.GetWalletsByClientIdAsync(_requestContext.ClientId);

            if (wallets == null)
                return NotFound();

            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            return Ok(wallets.Select(wallet => new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type,
                Description = wallet.Description,
                ApiKey = clientKeys.FirstOrDefault(x => x.Wallet == wallet.Id)?.Key
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
            var wallet = await _clientAccountClient.GetWalletAsync(id);

            if (wallet == null)
                return NotFound();

            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            return Ok(new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type,
                Description = wallet.Description,
                ApiKey = clientKeys.FirstOrDefault(x => x.Wallet == wallet.Id)?.Key
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

            var wallets = await _clientAccountClient.GetWalletsByClientIdAsync(_requestContext.ClientId);
            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            foreach (var wallet in wallets)
            {
                var balances = await _balancesClient.GetClientBalances(wallet.Type == TradingWalletType ? _requestContext.ClientId : wallet.Id);
                result.Add(new WalletBalancesModel
                {
                    Id = wallet.Id,
                    Type = wallet.Type,
                    Name = wallet.Name,
                    Description = wallet.Description,
                    Balances = balances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0],
                    ApiKey = clientKeys.FirstOrDefault(x => x.Wallet == wallet.Id)?.Key
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
            var clientBalances = await _balancesClient.GetClientBalances(_requestContext.ClientId);

            return Ok(clientBalances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0]);
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
            var clientBalances = await _balancesClient.GetClientBalances(walletId);
            if (clientBalances == null)
            {
                var wallet = await _clientAccountClient.GetWalletAsync(walletId);
                if (wallet == null)
                    return NotFound();
            }

            return Ok(clientBalances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0]);
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

            var wallets = await _clientAccountClient.GetWalletsByClientIdAsync(_requestContext.ClientId);
            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            foreach (var wallet in wallets)
            {
                var balance = await _balancesClient.GetClientBalanceByAssetId(
                    new ClientBalanceByAssetIdModel
                    {
                        ClientId = wallet.Type == TradingWalletType ? _requestContext.ClientId : wallet.Id,
                        AssetId = assetId
                    });
                result.Add(new WalletAssetBalanceModel
                {
                    Id = wallet.Id,
                    Type = wallet.Type,
                    Name = wallet.Name,
                    Description = wallet.Description,
                    Balances = balance != null ? ClientBalanceResponseModel.Create(balance) : new ClientBalanceResponseModel { AssetId = assetId, Balance = 0 },
                    ApiKey = clientKeys.FirstOrDefault(x => x.Wallet == wallet.Id)?.Key
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
            var clientBalanceResult = await _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = _requestContext.ClientId,
                    AssetId = assetId
                });

            if (clientBalanceResult != null && string.IsNullOrEmpty(clientBalanceResult.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResult));
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
            var clientBalanceResult = await _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = walletId,
                    AssetId = assetId
                });

            if (clientBalanceResult != null && string.IsNullOrEmpty(clientBalanceResult.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResult));
            }

            return NotFound();
        }
    }
}