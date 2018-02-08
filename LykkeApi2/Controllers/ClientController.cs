using System;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Common;
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

namespace LykkeApi2.Controllers
{
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/client")]
    public class ClientController : Controller
    {
        private readonly ILog _log;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly ClientAccountLogic _clientAccountLogic;
        private readonly IRequestContext _requestContext;
        private readonly IPersonalDataService _personalDataService;

        public ClientController(
            ILog log,
            ILykkeRegistrationClient lykkeRegistrationClient,
            ClientAccountLogic clientAccountLogic,
            IRequestContext requestContext,
            IPersonalDataService personalDataService)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _lykkeRegistrationClient = lykkeRegistrationClient ?? throw new ArgumentNullException(nameof(lykkeRegistrationClient));
            _clientAccountLogic = clientAccountLogic;
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _personalDataService = personalDataService ?? throw new ArgumentNullException(nameof(personalDataService));
        }

        /// <summary>
        /// Register a new client.
        /// </summary>
        [HttpPost("register")]
        [SwaggerOperation("RegisterClient")]
        [ProducesResponseType(typeof(AccountsRegistrationResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody]AccountRegistrationModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!model.Email.IsValidEmailAndRowKey())
            {
                ModelState.AddModelError("email", Phrases.InvalidEmailFormat);
                return BadRequest(ModelState);
            }
            
            if (await _clientAccountLogic.IsTraderWithEmailExistsForPartnerAsync(model.Email, model.PartnerId))
            {
                ModelState.AddModelError("email", Phrases.ClientWithEmailIsRegistered);
                return BadRequest(ModelState);
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
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Auth([FromBody]AuthRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!model.Email.IsValidEmailAndRowKey())
            {
                ModelState.AddModelError("email", Phrases.InvalidEmailFormat);
                return BadRequest(ModelState);
            }
            
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
                LastName = personalData?.LastName
            });
        }
    }
}
