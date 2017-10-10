using System.Collections.Generic;
using Common.Log;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Microsoft.AspNetCore.Authorization;
using ClientBalanceResponseModel = LykkeApi2.Models.ClientBalancesModels.ClientBalanceResponseModel;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/[controller]")]
    public class ClientBalancesController : Controller
    {
        private readonly ILog _log;
        private readonly IBalancesClient _balancesClient;

        public ClientBalancesController(ILog log,
            IBalancesClient balancesClient)
        {
            _log = log;
            _balancesClient = balancesClient;
        }

        [HttpGet]
        [SwaggerOperation("GetClientBalances")]
        [ProducesResponseType(typeof(IEnumerable<ClientBalanceResponseModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get()
        {
            var clientBalances = await _balancesClient.GetClientBalances(this.GetClientId());

            if (clientBalances == null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, Phrases.ClientBalanceNotFound));
            }

            return Ok(clientBalances);
        }

        [HttpGet("{assetId}")]
        [SwaggerOperation("GetClientBalanceByAssetId")]
        [ProducesResponseType(typeof(ClientBalanceResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetClientBalanceByAssetId(string assetId)
        {
            var clientBalanceResult = await _balancesClient.GetClientBalanceByAssetId(
                new ClientBalanceByAssetIdModel
                {
                    ClientId = this.GetClientId(),
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