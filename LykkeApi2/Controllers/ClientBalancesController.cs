using Common.Log;
using Lykke.Service.Wallets.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ClientBalancesModels;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    //[Authorize]
    [LowerVersion(Devices = "IPhone,IPad", LowerVersion = 181)]
    [LowerVersion(Devices = "android", LowerVersion = 659)]
    [Route("api/[controller]")]
    public class ClientBalancesController : Controller
    {
        private readonly ILog _log;
        private readonly IWalletsClient _walletsClient;

        public ClientBalancesController(ILog log,
            IWalletsClient walletsClient)
        {
            _log = log;
            _walletsClient = walletsClient;
        }

        [HttpGet]
        [SwaggerOperation("GetClientBalances")]
        public async Task<IActionResult> Get()
        {
            var clientId = this.GetClientId();

            var clientBalances = await _walletsClient.GetClientBalances("35302a53-cacb-4052-b5c0-57f9c819495b");

            if (clientBalances == null)
            {
                return NotFound(new ApiResponse(ResponseStatusCode.NotFound, Phrases.ClientBalanceNotFound));
            }

            return Ok(clientBalances);
        }

        [HttpGet("{assetId}")]
        [SwaggerOperation("GetClientBalanceByAssetId")]
        public async Task<IActionResult> GetClientBalnceByAssetId(string assetId)
        {
            var clientId = this.GetClientId();

            var clientBalanceResult = await _walletsClient.GetClientBalanceByAssetId(
                        new Lykke.Service.Wallets.Client.AutorestClient.Models.ClientBalanceByAssetIdModel()
                        {
                            ClientId = "35302a53-cacb-4052-b5c0-57f9c819495b",
                            AssetId = assetId
                        });

            if (clientBalanceResult != null && string.IsNullOrEmpty(clientBalanceResult.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResult));
            }
            else
            {
                return NotFound(new ApiResponse(ResponseStatusCode.InternalServerError, clientBalanceResult.ErrorMessage ?? Phrases.ClientBalanceNotFound));
            }
        }
    }
}

