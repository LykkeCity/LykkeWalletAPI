using Common.Log;
using Lykke.Service.Wallets.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ClientBalancesModels;
using LykkeApi2.Models.ResponceModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System.Net;
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
            //var clientId = this.GetClientId();
            var clientId = "e9b10277-aa24-4fa2-90d6-9c6756d88f81"; //has wallets client id

            var clientBalances = await _walletsClient.GetClientBalances(clientId);

            if (clientBalances == null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound, Phrases.ClientBalanceNotFound));
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
                            ClientId = clientId,
                            AssetId = assetId
                        });

            if (clientBalanceResult != null && string.IsNullOrEmpty(clientBalanceResult.ErrorMessage))
            {
                return Ok(ClientBalanceResponseModel.Create(clientBalanceResult));
            }
            else
            {
                return NotFound(new ApiResponse(HttpStatusCode.InternalServerError, clientBalanceResult.ErrorMessage ?? Phrases.ClientBalanceNotFound));
            }
        }
    }
}

