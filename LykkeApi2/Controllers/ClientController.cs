using System;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Constants;
using Core.Settings;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Models.Auth;
using LykkeApi2.Models.ClientAccountModels;
using Microsoft.AspNetCore.Authorization;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Session.Client;
using LykkeApi2.Models.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using LykkeApi2.Models;
using LykkeApiErrorResponse = Lykke.Common.ApiLibrary.Contract.LykkeApiErrorResponse;

namespace LykkeApi2.Controllers
{
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/client")]
    [ApiController]
    public class ClientController : Controller
    {
        private readonly ILog _log;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly ClientAccountLogic _clientAccountLogic;
        private readonly IKycStatusService _kycStatusService;
        private readonly IRequestContext _requestContext;
        private readonly IPersonalDataService _personalDataService;
        private readonly IClientAccountClient _clientAccountService;
        private readonly BaseSettings _baseSettings;

        public ClientController(
            ILog log,
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
        [Obsolete(@"In order to get access to an api, you should go through OAuth. 
                    It's accessible through 'Autorize' button from the right-top corner of this page.
                    If you need to get access tokens for your client app, then you should build an integration with Lykke OAuth server.")]
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
            var session = await _clientSessionsClient.GetAsync(_requestContext.SessionId);
            
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

            await _clientSessionsClient.CreateTradingSession(_requestContext.SessionId, TimeSpan.FromMilliseconds(request.Ttl));

            return Ok();
        }

        [Authorize]
        [HttpPatch("session")]
        public async Task<IActionResult> ExtendTradingSession([FromBody]TradingModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessage());

            await _clientSessionsClient.ExtendTradingSession(_requestContext.SessionId, TimeSpan.FromMilliseconds(request.Ttl));

            return Ok();
        }

        [Authorize]
        [HttpGet("userInfo")]
        [SwaggerOperation("UserInfo")]
        [ProducesResponseType(typeof(UserInfoResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UserInfo()
        {
            try
            {
                var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);

                return Ok(new UserInfoResponseModel
                {
                    Email = personalData?.Email,
                    FirstName = personalData?.FirstName,
                    LastName = personalData?.LastName,
                    KycStatus = (await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId)).ToApiModel()
                });
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(ClientController), nameof(UserInfo),
                    $"clientId = {_requestContext.ClientId}", e);

                return StatusCode((int) HttpStatusCode.InternalServerError);
            }
        }

        [Authorize]
        [HttpGet("features")]
        [SwaggerOperation("Features")]
        [ProducesResponseType(typeof(FeaturesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<FeaturesResponseModel> Features()
        {
            var features = await _clientAccountService.GetFeaturesAsync(_requestContext.ClientId);
            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);

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

        /// <summary>
        /// Get date of birth
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("dob")]
        [SwaggerOperation("GetDateOfBirth")]
        [ProducesResponseType(typeof(DateOfBirthResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetDateOfBirth()
        {
            var clientId = _requestContext.ClientId;

            var personalData = await _personalDataService.GetAsync(clientId);

            if (personalData == null)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.ClientNotFound);

            return Ok(new DateOfBirthResponseModel {DateOfBirth = personalData.DateOfBirth});
        }

        /// <summary>
        /// Update date of birth
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("dob")]
        [SwaggerOperation("UpdateDateOfBirth")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateDateOfBirth([FromBody] DateOfBirthModel model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get address
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("address")]
        [SwaggerOperation("GetAddress")]
        [ProducesResponseType(typeof(AddressResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAddress()
        {
            var clientId = _requestContext.ClientId;

            var personalData = await _personalDataService.GetAsync(clientId);

            if (personalData == null)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.ClientNotFound);

            return Ok(new AddressResponseModel {Address = personalData.Address});
        }

        /// <summary>
        /// Update address
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("address")]
        [SwaggerOperation("UpdateAddress")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateAddress([FromBody] AddressModel model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get zip code
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("zip")]
        [SwaggerOperation("GetZipCode")]
        [ProducesResponseType(typeof(ZipCodeResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetZipCode()
        {
            var clientId = _requestContext.ClientId;

            var personalData = await _personalDataService.GetAsync(clientId);

            if (personalData == null)
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.ClientNotFound);

            return Ok(new ZipCodeResponseModel {Zip = personalData.Zip});
        }

        /// <summary>
        /// Update zip/postal code
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("zip")]
        [SwaggerOperation("UpdateZipCode")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateZipCode([FromBody] ZipCodeModel model)
        {
            throw new NotImplementedException();
        }
    }
}
