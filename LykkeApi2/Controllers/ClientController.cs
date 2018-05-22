using System;
using System.ComponentModel.DataAnnotations;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Identity;
using Core.Settings;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Models.Auth;
using LykkeApi2.Models.ClientAccountModels;
using Microsoft.AspNetCore.Authorization;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using LykkeApi2.Infrastructure.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Session.Client;
using LykkeApi2.Models.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using LykkeApi2.Models;

namespace LykkeApi2.Controllers
{
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/client")]
    public class ClientController : Controller
    {
        private readonly ILog _log;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly ILykkePrincipal _lykkePrincipal;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly ClientAccountLogic _clientAccountLogic;
        private readonly IKycStatusService _kycStatusService;
        private readonly IRequestContext _requestContext;
        private readonly IPersonalDataService _personalDataService;
        private readonly IClientAccountClient _clientAccountService;
        private readonly BaseSettings _baseSettings;

        public ClientController(
            ILog log,
            ILykkePrincipal lykkePrincipal,
            IClientSessionsClient clientSessionsClient,
            ILykkeRegistrationClient lykkeRegistrationClient,
            ClientAccountLogic clientAccountLogic,            
            IRequestContext requestContext,
            IPersonalDataService personalDataService,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountService, BaseSettings baseSettings)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _lykkeRegistrationClient = lykkeRegistrationClient ?? throw new ArgumentNullException(nameof(lykkeRegistrationClient));
            _lykkePrincipal = lykkePrincipal;
            _clientSessionsClient = clientSessionsClient;
            _clientAccountLogic = clientAccountLogic;
            _kycStatusService = kycStatusService;
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _personalDataService = personalDataService ?? throw new ArgumentNullException(nameof(personalDataService));
            _clientAccountService = clientAccountService;
            _baseSettings = baseSettings;
        }

        /// <summary>
        /// Register a new client.
        /// </summary>
        [HttpPost("register")]
        [SwaggerOperation("RegisterClient")]
        [ProducesResponseType(typeof(AccountsRegistrationResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody]AccountRegistrationModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessage());

            if (!model.Email.IsValidEmailAndRowKey())
                return BadRequest(Phrases.InvalidEmailFormat);
            
            if (await _clientAccountLogic.IsTraderWithEmailExistsForPartnerAsync(model.Email, model.PartnerId))
                return BadRequest(Phrases.ClientWithEmailIsRegistered);

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

            var result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

            if (result == null)
                return BadRequest(Phrases.TechnicalProblems);

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
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Auth([FromBody]AuthRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessage());

            if (!model.Email.IsValidEmailAndRowKey())
                return BadRequest(Phrases.InvalidEmailFormat);
            
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
                NotificationsId = authResult?.NotificationsId
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> LogOut()
        {
            var token = _lykkePrincipal.GetToken();
            
            var session = await _clientSessionsClient.GetAsync(token);
            
            if (session != null)
                await _clientSessionsClient.DeleteSessionIfExistsAsync(session.SessionToken);
            
            return Ok();
        }

        [Authorize]
        [HttpPost("session")]
        public async Task<IActionResult> CreateTradingSession([FromBody]TradingModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessage());

            await _clientSessionsClient.CreateTradingSession(_lykkePrincipal.GetToken(), TimeSpan.FromMilliseconds(request.Ttl));

            return Ok();
        }

        [Authorize]
        [HttpPatch("session")]
        public async Task<IActionResult> ExtendTradingSession([FromBody]TradingModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessage());

            await _clientSessionsClient.ExtendTradingSession(_lykkePrincipal.GetToken(), TimeSpan.FromMilliseconds(request.Ttl));

            return Ok();
        }

        [Authorize]
        [HttpGet("userInfo")]
        [SwaggerOperation("UserInfo")]
        [ProducesResponseType(typeof(UserInfoResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UserInfo()
        {
            IPersonalData personalData;

            try
            {
                personalData = await _personalDataService.GetAsync(_requestContext.ClientId);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(ClientController), nameof(UserInfo),
                    $"clientId = {_requestContext.ClientId}", e);

                return StatusCode((int) HttpStatusCode.InternalServerError);
            }

            return Ok(new UserInfoResponseModel
            {
                Email = personalData?.Email,
                FirstName = personalData?.FirstName,
                LastName = personalData?.LastName,
                KycStatus = (await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId)).ToApiModel()
            });
        }

        [Authorize]
        [HttpGet("features")]
        [SwaggerOperation("Features")]
        [ProducesResponseType(typeof(FeaturesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<FeaturesResponseModel> Features()
        {
            var features = await _clientAccountService.GetFeaturesAsync(_requestContext.ClientId);
            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());

            return new FeaturesResponseModel
            {
                AffiliateEnabled = features.AffiliateEnabled,
                TradingSession = new TradingSessionResponseModel
                {
                    Enabled = _baseSettings.EnableSessionValidation,
                    Confirmed = tradingSession?.Confirmed,
                    Ttl = tradingSession?.Ttl?.TotalMilliseconds
                }
            };
        }               
    }
}
