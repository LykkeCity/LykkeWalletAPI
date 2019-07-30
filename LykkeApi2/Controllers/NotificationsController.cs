using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core.Constants;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
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
            ILog log
            )
        {
            _requestContext = requestContext;
            _clientAccountClient = clientAccountClient;
            _pushNotificationsClient = pushNotificationsClient;
            _log = log;
        }

        /// <summary>
        /// Register installation in notifications hub
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("push")]
        [SwaggerOperation("RegisterInstallation", "Register device installation in notification hub")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterInstallation([FromBody] PushRegistrationModel model)
        {
            var clientId = _requestContext.ClientId;

            ClientAccountInformationModel client = await _clientAccountClient.GetClientByIdAsync(clientId);

            try
            {
                await _pushNotificationsClient.Installations.RegisterAsync(new InstallationModel
                {
                    NotificationId = client.NotificationsId,
                    Platform = model.Platform,
                    PushChannel = model.PushChannel
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);
            }

            return Ok();
        }

        /// <summary>
        /// Remove installation from notifications hub
        /// </summary>
        /// <param name="pushChannel">PNS handle</param>
        /// <returns></returns>
        [HttpDelete("push")]
        [SwaggerOperation("RemoveInstallation", "Remove device installation from notification hub")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RemoveInstallation(string pushChannel)
        {
            var clientId = _requestContext.ClientId;

            ClientAccountInformationModel client = await _clientAccountClient.GetClientByIdAsync(clientId);

            var installation =
                (await _pushNotificationsClient.Installations.GetByNotificationIdAsync(client.NotificationsId))
                .FirstOrDefault(x => x.PushChannel == pushChannel);

            if (installation == null)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InstallationNotFound);

            await _pushNotificationsClient.Installations.RemoveAsync(new InstallationRemoveModel
            {
                NotificationId = client.NotificationsId,
                InstallationId = installation.InstallationId
            });

            return Ok();
        }
    }
}
