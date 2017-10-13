using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Models.Auth;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ClientBalancesModels;
using Microsoft.AspNetCore.Authorization;
using ClientBalanceResponseModel = LykkeApi2.Models.ClientBalancesModels.ClientBalanceResponseModel;

namespace LykkeApi2.Controllers
{
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/client")]
    public class ClientController : Controller
    {
        private readonly ILog _log;
        private readonly IBalancesClient _balancesClient;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly ClientAccountLogic _clientAccountLogic;
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountService _clientAccountService;

        public ClientController(
            ILog log,
            IBalancesClient balancesClient,
            ILykkeRegistrationClient lykkeRegistrationClient,
            ClientAccountLogic clientAccountLogic,
            IRequestContext requestContext,
            IClientAccountService clientAccountService)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _balancesClient = balancesClient ?? throw new ArgumentNullException(nameof(balancesClient));
            _lykkeRegistrationClient = lykkeRegistrationClient ?? throw new ArgumentNullException(nameof(lykkeRegistrationClient));
            _clientAccountLogic = clientAccountLogic;
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _clientAccountService = clientAccountService ?? throw new ArgumentNullException(nameof(clientAccountService));
        }

        /// <summary>
        /// Register a new client.
        /// </summary>
        [HttpPost("register")]
        [SwaggerOperation("RegisterClient")]
        [ProducesResponseType(typeof(AccountsRegistrationResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiBadRequestResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Post([FromBody]AccountRegistrationModel model)
        {
            if (await _clientAccountLogic.IsTraderWithEmailExistsForPartnerAsync(model.Email, model.PartnerId))
            {
                ModelState.AddModelError("email", Phrases.ClientWithEmailIsRegistered);
                return BadRequest(new ApiBadRequestResponse(ModelState));
            }

            var registrationModel = new RegistrationModel
            {
                ClientInfo = model.ClientInfo,
                UserAgent = _requestContext.UserAgent,
                ContactPhone = model.ContactPhone,
                Email = model.Email,
                FullName = model.FullName,
                Hint = model.Hint,
                PartnerId = model.PartnerId,
                Password = model.Password,
                Ip = _requestContext.GetIp(),
                Changer = RecordChanger.Client,
                IosVersion = _requestContext.IsIosDevice ? _requestContext.Version : null,
                Referer = HttpContext.Request.Host.Host
            };

            RegistrationResponse result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

            if (result == null)
                return NotFound(new ApiResponse(HttpStatusCode.InternalServerError, Phrases.TechnicalProblems));

            return Ok(new AccountsRegistrationResponseModel
            {
                Token = result.Token,
                NotificationsId = result.NotificationsId,
                PersonalData = new ApiPersonalDataModel
                {
                    FullName = result.PersonalData.FullName,
                    Email = result.PersonalData.Email,
                    Phone = result.PersonalData.Phone
                },
                CanCashInViaBankCard = result.CanCashInViaBankCard,
                SwiftDepositEnabled = result.SwiftDepositEnabled
            });
        }

        /// <summary>
        /// Authenticate.
        /// </summary>
        [HttpPost("auth")]
        [SwaggerOperation("Auth")]
        [ProducesResponseType(typeof(AuthResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Auth([FromBody]AuthRequestModel model)
        {
            var authResult = await _lykkeRegistrationClient.AuthorizeAsync(new AuthModel
            {
                ClientInfo = model.ClientInfo,
                Email = model.Email,
                Password = model.Password,
                Ip = _requestContext.GetIp(),
                UserAgent = _requestContext.UserAgent,
                PartnerId = model.PartnerId
            });

            if (authResult?.Status == AuthenticationStatus.Error)
                return BadRequest(new { message = authResult.ErrorMessage });

            return Ok(new AuthResponseModel
            {
                AccessToken = authResult?.Token,
            });
        }

        /// <summary>
        /// Get all wallets balances.
        /// </summary>
        [Authorize]
        [HttpGet("balances")]
        [SwaggerOperation("GetBalances")]
        [ProducesResponseType(typeof(IEnumerable<WalletBalancesModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get()
        {
            var result = new List<WalletBalancesModel>();
            var clientBalances = await _balancesClient.GetClientBalances(_requestContext.ClientId);
            result.Add(new WalletBalancesModel
            {
                Id = _requestContext.ClientId,
                Type = "Trading",
                Balances = clientBalances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0]
            });

            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(_requestContext.ClientId);
            if (wallets != null)
            {
                foreach (var wallet in wallets)
                {
                    var balances = await _balancesClient.GetClientBalances(wallet.Id);
                    result.Add(new WalletBalancesModel
                    {
                        Id = wallet.Id,
                        Type = wallet.Type,
                        Name = wallet.Name,
                        Balances = balances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0]
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Get trading wallet balances.
        /// </summary>
        [Authorize]
        [HttpGet("trading/balances")]
        [SwaggerOperation("GetClientBalance")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetClientBalance()
        {
            var clientBalances = await _balancesClient.GetClientBalances(_requestContext.ClientId);

            return Ok(clientBalances?.Select(ClientBalanceResponseModel.Create) ?? new ClientBalanceResponseModel[0]);
        }

        /// <summary>
        /// Get specified wallet balances.
        /// </summary>
        [Authorize]
        [HttpGet("{walletId}/balances")]
        [SwaggerOperation("GetWalletBalance")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetWalletBalance(string walletId)
        {
            var clientBalances = await _balancesClient.GetClientBalances(walletId);
            if (clientBalances == null)
            {
                var wallet = await _clientAccountService.GetWalletAsync(walletId);
                if (wallet == null)
                    return NotFound(new ApiResponse(HttpStatusCode.NotFound, Phrases.ClientBalanceNotFound));
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
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetBalancesByAssetId(string assetId)
        {
            var result = new List<WalletAssetBalanceModel>();
            var clientBalance = await _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = _requestContext.ClientId,
                    AssetId = assetId
                });

            if (!string.IsNullOrEmpty(clientBalance?.ErrorMessage))
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, clientBalance.ErrorMessage ?? Phrases.ClientBalanceNotFound));
            }

            result.Add(new WalletAssetBalanceModel
            {
                Id = _requestContext.ClientId,
                Type = "Trading",
                Balances = clientBalance != null ? ClientBalanceResponseModel.Create(clientBalance) : null
            });

            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(_requestContext.ClientId);
            if (wallets != null)
            {
                foreach (var wallet in wallets)
                {
                    var balance = await _balancesClient.GetClientBalanceByAssetId(
                        new ClientBalanceByAssetIdModel
                        {
                            ClientId = wallet.Id,
                            AssetId = assetId
                        });
                    result.Add(new WalletAssetBalanceModel
                    {
                        Id = wallet.Id,
                        Type = wallet.Type,
                        Name = wallet.Name,
                        Balances = balance != null ? ClientBalanceResponseModel.Create(balance) : null
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Get trading wallet balances by asset id.
        /// </summary>
        [Authorize]
        [HttpGet("trading/balances/{assetId}")]
        [SwaggerOperation("GetClientBalanceByAssetId")]
        [ProducesResponseType(typeof(ClientBalanceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetClientBalanceByAssetId(string assetId)
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

            return NotFound(new ApiResponse(HttpStatusCode.NotFound,
                clientBalanceResult?.ErrorMessage ?? Phrases.ClientBalanceNotFound));
        }

        /// <summary>
        /// Get specified wallet balances by asset id.
        /// </summary>
        [Authorize]
        [HttpGet("{walletId}/balances/{assetId}")]
        [SwaggerOperation("GetWalletBalanceByAssetId")]
        [ProducesResponseType(typeof(ClientBalanceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
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

            return NotFound(new ApiResponse(HttpStatusCode.NotFound,
                clientBalanceResult?.ErrorMessage ?? Phrases.ClientBalanceNotFound));
        }
    }
}