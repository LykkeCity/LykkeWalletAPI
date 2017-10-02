using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.ClientAccount.Client.AutorestClient.Models;
using Lykke.Service.Wallets.Client.AutorestClient;
using LykkeApi2.Models.Wallets;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using CreateWalletRequest = LykkeApi2.Models.Wallets.CreateWalletRequest;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class WalletController : Controller
    {
        private readonly IClientAccountService _clientAccountService;
        private readonly IWalletsService _walletsService;

        public WalletController(IClientAccountService clientAccountService, IWalletsService walletsService)
        {
            _clientAccountService = clientAccountService;
            _walletsService = walletsService;
        }
                
        [HttpPost]
        [SwaggerOperation("CreateWallet")]
        public async Task<WalletModel> CreateWallet([FromBody] CreateWalletRequest request)
        {
            var wallet = await _clientAccountService.CreateWalletAsync(
                new Lykke.Service.ClientAccount.Client.AutorestClient.Models.CreateWalletRequest(request.ClientId, request.Type, request.Name));

            return new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type };
        }
            
        [HttpGet("{id}")]
        [SwaggerOperation("GetWallet")]
        [ProducesResponseType(typeof(WalletModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWallet(string id)
        {
            var wallet = await _clientAccountService.GetWalletAsync(id);

            if (wallet == null)
                return NotFound();

            return Ok(new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type }); 
        }

       
        [ProducesResponseType(typeof(IEnumerable<WalletDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("client/{id}")]
        [SwaggerOperation("GetWalletsByClientId")]
        public async Task<IActionResult> GetWalletsByClientId(string id)
        {
            var wallets = await _clientAccountService.GetWalletsByClientIdAsync(id);

            if (wallets == null)
                return NotFound();
                        
            return Ok(wallets.Select(wallet => new WalletModel { Id = wallet.Id, Name = wallet.Name, Type = wallet.Type }));
        }

        [ProducesResponseType(typeof(IEnumerable<BalanceModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{id}/balances")]
        [SwaggerOperation("GetBalances")]
        public async Task<IActionResult> GetBalances(string id)
        {
            var balances = await _walletsService.GetClientBalancesAsync(id);
            
            return Ok(balances);
        }
    }
}