using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Repositories;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/features")]
    [ApiController]
    public class FeaturesController : Controller
    {
        private readonly IFeaturesRepository _featuresRepository;
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountClient; 
        private readonly PrivateWalletsSettings _privateWalletsSettings;

        public FeaturesController(IFeaturesRepository featuresRepository, IRequestContext requestContext, IClientAccountClient clientAccountClient, PrivateWalletsSettings privateWalletsSettings)
        {
            _featuresRepository = featuresRepository;
            _requestContext = requestContext;
            _clientAccountClient = clientAccountClient;
            this._privateWalletsSettings = privateWalletsSettings;
        }

        /// <summary>
        /// Get all features and their enabled/disabled status for specific client.
        /// If there is no entry for specific client, global setting will be used.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(IDictionary<string, bool>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var clientId = _requestContext.ClientId;
            var featureFlags = await _featuresRepository.GetAll(clientId);
            
            if(featureFlags.TryGetValue(WellKnownFeatureFlags.PrivateWallets, out var isPrivateWalletsEnabledGlobally) && isPrivateWalletsEnabledGlobally)
            {
                var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(clientId);
                //disable private wallets for new clients
                if(client == null || client.Registered > _privateWalletsSettings.DisableForRegisteredAfter)
                {
                    featureFlags[WellKnownFeatureFlags.PrivateWallets] = false;
                }
            }
            return Ok(await _featuresRepository.GetAll(clientId));
        }
    }
}
