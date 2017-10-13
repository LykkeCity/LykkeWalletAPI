using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using CreateWalletRequest = LykkeApi2.Models.Wallets.CreateWalletRequest;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/client")]
    public class WalletController : Controller
    {
        private readonly IClientAccountService _clientAccountService;
        private readonly IRequestContext _requestContext;

        public WalletController(IClientAccountService clientAccountService, IRequestContext requestContext)
        {
            _clientAccountService = clientAccountService;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Create wallet.
        /// </summary>
        [HttpPost("wallet")]
        [SwaggerOperation("CreateWallet")]
        [ApiExplorerSettings(GroupName = "Client")]
        public async Task<WalletModel> CreateWallet([FromBody] CreateWalletRequest request)
        {
            var wallet = await _clientAccountService.CreateWalletAsync(
                new Lykke.Service.ClientAccount.Client.AutorestClient.Models.CreateWalletRequest(_requestContext.ClientId, request.Type, request.Name));

            return new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type };
        }

        /// <summary>
        /// Get all client wallets.
        /// </summary>
        [HttpGet("wallets")]
        [ProducesResponseType(typeof(IEnumerable<WalletDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [SwaggerOperation("GetClientWallets")]
        [ApiExplorerSettings(GroupName = "Client")]
        public async Task<IActionResult> GetClientWallets()
        {
            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(_requestContext.ClientId);

            if (wallets == null)
                return NotFound();

            return Ok(wallets.Select(wallet => new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type }));
        }

        /// <summary>
        /// Get specified wallet.
        /// </summary>
        [HttpGet("wallet/{id}")]
        [SwaggerOperation("GetWallet")]
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        [ApiExplorerSettings(GroupName = "Client")]
        public async Task<IActionResult> GetWallet(string id)
        {
            var wallet = await _clientAccountService.GetWalletAsync(id);

            if (wallet == null)
                return NotFound();

            return Ok(new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type });
        }
    }
}