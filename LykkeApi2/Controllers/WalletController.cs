using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.Client;
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
        private readonly IBalancesClient _balanceService;
        private readonly IRequestContext _requestContext;

        public WalletController(IClientAccountService clientAccountService, IBalancesClient balanceService, IRequestContext requestContext)
        {
            _clientAccountService = clientAccountService;
            _balanceService = balanceService;
            _requestContext = requestContext;
        }
                        
        [HttpPost("wallet")]
        [SwaggerOperation("CreateWallet")]
        [ApiExplorerSettings(GroupName = "Client")]
        public async Task<WalletModel> CreateWallet([FromBody] CreateWalletRequest request)
        {
            var apiRequest =
                new Lykke.Service.ClientAccount.Client.AutorestClient.Models.CreateWalletRequest(
                    _requestContext.ClientId, request.Name);
            var wallet = await _clientAccountService.CreateWalletAsync(apiRequest);

            return new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type };
        }
        
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

        [HttpGet("wallet/{id}/balances")]
        [ProducesResponseType(typeof(IEnumerable<BalanceModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]                
        [ApiExplorerSettings(GroupName = "Client")]
        public async Task<IActionResult> GetBalances(string id)
        {
            var balances = await _balanceService.GetClientBalances(id);
            
            return Ok(balances);
        }
    }
}