using System;
using System.Collections.Generic;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Net;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Models;
using LykkeApi2.Models.Auth;
using LykkeApi2.Models.ClientAccountModels;
using Microsoft.AspNetCore.Authorization;
using ClientBalanceResponseModel = LykkeApi2.Models.ClientBalancesModels.ClientBalanceResponseModel;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/client")]
    public partial class ClientController : Controller
    {
        private readonly ILog _log;
        private readonly IBalancesClient _balancesClient;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ClientAccountLogic _clientAccountLogic;
        private readonly IRequestContext _requestContext;

        public ClientController(
            ILog log,
            IBalancesClient balancesClient, 
            ILykkeRegistrationClient lykkeRegistrationClient,
            IClientAccountClient clientAccountClient,
            ClientAccountLogic clientAccountLogic,
            IRequestContext requestContext)
        {
            _log = log;
            _balancesClient = balancesClient;
            _lykkeRegistrationClient = lykkeRegistrationClient;
            _clientAccountClient = clientAccountClient;
            _clientAccountLogic = clientAccountLogic;
            _requestContext = requestContext;
        }

        [HttpGet("exist")]
        public async Task<ResponseModel<Models.ClientAccountModels.AccountExistResultModel>> Get([FromQuery]string email)
        {
            if (string.IsNullOrEmpty(email))
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", Phrases.FieldShouldNotBeEmpty);

            email = email.ToLower();

            if (!email.IsValidEmail())
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", Phrases.InvalidAddress);

            var result = false;
            try
            {
                result = await _clientAccountClient.CheckIfAccountExists(email);

                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateOk(
                    new Models.ClientAccountModels.AccountExistResultModel { IsEmailRegistered = result });
            }
            catch (Exception ex)
            {
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", ex.Message);
            }
        }

        [HttpPost("register")]
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
                IosVersion = _requestContext.IsIosDevice() ? _requestContext.GetVersion() : null,
                Referer = HttpContext.Request.Host.Host
            };

            RegistrationResponse result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

            if (result == null)
                return NotFound(new ApiResponse(HttpStatusCode.InternalServerError, Phrases.TechnicalProblems));

            var resultPhone = await _clientAccountClient.InsertIndexedByPhoneAsync(
                new IndexByPhoneRequestModel()
                {
                    ClientId = result.Account.Id,
                    PhoneNumber = result.PersonalData.Phone,
                    PreviousPhoneNumber = null
                });

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

        [HttpPost("auth")]
        public async Task<IActionResult> Auth([FromBody]AuthRequestModel model)
        {
            AuthResponse authResult = await _lykkeRegistrationClient.AuthorizeAsync(new AuthModel
            {
                ClientInfo = model.ClientInfo,
                Email = model.Email,
                Password = model.Password,
                Ip = _requestContext.GetIp(),
                UserAgent = _requestContext.UserAgent,
                PartnerId = model.PartnerId
            });

            if (authResult.Status == AuthenticationStatus.Error)
                return BadRequest(new { message = authResult.ErrorMessage });

            return Ok(new AuthResponseModel
            {
                AccessToken = authResult.Token,
            });
        }        

        [HttpGet("balances")]        
        [SwaggerOperation("GetBalances")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get()
        {
            var clientBalances = await _balancesClient.GetClientBalances(_requestContext.ClientId);

            if (clientBalances == null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, Phrases.ClientBalanceNotFound));
            }

            return Ok(clientBalances);
        }

        [HttpGet("balances/{assetId}")]
        [SwaggerOperation("GetBalanceByAssetId")]
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
    }
}