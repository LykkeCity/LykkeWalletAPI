using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Dialogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/dialogs")]
    [ApiController]
    public class DialogsController : Controller
    {
        private readonly IClientDialogsClient _clientDialogsClient;
        private readonly IRequestContext _requestContext;

        public DialogsController(
            IClientDialogsClient clientDialogsClient,
            IRequestContext requestContext)
        {
            _clientDialogsClient = clientDialogsClient;
            _requestContext = requestContext;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClientDialogResponseModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllPendingDialogs()
        {
            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(_requestContext.ClientId);

            return Ok(pendingDialogs.Select(x => x.ToApiModel()));
        }

        [HttpPost("{dialogId}/actions/{actionId}")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> SubmitPendingDialog([FromRoute] string dialogId, [FromRoute] string actionId)
        {
            var dialog = await _clientDialogsClient.ClientDialogs.GetDialogAsync(_requestContext.ClientId, dialogId);

            if (dialog == null || dialog.Actions.All(action => action.Id != actionId))
            {
                return NotFound();
            }

            await _clientDialogsClient.Dialogs.SubmitDialogAsync(new SubmitDialogRequest
            {
                ClientId = _requestContext.ClientId,
                DialogId = dialogId,
                ActionId = actionId
            });

            return Ok();
        }
    }
}