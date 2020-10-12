using System;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core.Constants;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PushNotifications.Client;
using Lykke.Service.PushNotifications.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPushNotificationsClient _pushNotificationsClient;
        private readonly ILog _log;

        public NotificationsController(
            IRequestContext requestContext,
            IClientAccountClient clientAccountClient,
            IPushNotificationsClient pushNotificationsClient,
            ILogFactory logFactory
            )
        {
            _requestContext = requestContext;
            _clientAccountClient = clientAccountClient;
            _pushNotificationsClient = pushNotificationsClient;
            _log = logFactory.CreateLog(this);
        }

        /// <summary>
        /// Register installation in notifications hub
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("push")]
        [SwaggerOperation("RegisterInstallation", "Register device installation in notification hub")]
        [ProducesResponseType(typeof(InstallationResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterInstallation([FromBody] PushRegistrationModel model)
        {
            var clientId = _requestContext.ClientId;

            var client = await _clientAccountClient.ClientAccountInformation.GetClientByIdAsync(clientId);

            try
            {
                InstallationResponse response = await _pushNotificationsClient.Installations.RegisterAsync(new InstallationModel
                {
                    ClientId = clientId,
                    InstallationId = model.InstallationId,
                    NotificationId = client.NotificationsId,
                    Platform = model.Platform,
                    PushChannel = model.PushChannel
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);
            }
        }
    }
}
