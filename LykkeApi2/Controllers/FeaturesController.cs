using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core.Repositories;
using Lykke.Common.Log;
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
        private readonly ILog _log;
        private readonly IFeaturesRepository _featuresRepository;
        private readonly IRequestContext _requestContext;
        private readonly IClientAccountClient _clientAccountClient; 
        private readonly PrivateWalletsSettings _privateWalletsSettings;

        public FeaturesController(ILogFactory logFacotry,
            IFeaturesRepository featuresRepository, 
            IRequestContext requestContext, 
            IClientAccountClient clientAccountClient, 
            PrivateWalletsSettings privateWalletsSettings)
        {
            _log = logFacotry.CreateLog(this);
            _featuresRepository = featuresRepository;
            _requestContext = requestContext;
            _clientAccountClient = clientAccountClient;
            _privateWalletsSettings = privateWalletsSettings;
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

            if(clientId == null)
            {
                _log.Warning(nameof(Get), "Client ID is null in the request context");
            }

            var featureFlags = await _featuresRepository.GetAll(clientId);
            if(featureFlags.TryGetValue(WellKnownFeatureFlags.PrivateWallets, out var isPrivateWalletsEnabledGlobally))
            {
                // is feature flag is enabled globally
                // additionally ensure that only old users can use this feature
                if(isPrivateWalletsEnabledGlobally)
                {
                    var client = await _clientAccountClient.ClientAccountInformation.GetByIdAsync(clientId);

                    if (clientId == null)
                    {
                        _log.Warning(nameof(Get), "Client Account information is not found", context: new
                        {
                            ClientId = clientId,
                        });
                    }

                    //disable private wallets for new clients
                    if (client == null || client.Registered > _privateWalletsSettings.DisableForRegisteredAfter)
                    {
                        featureFlags[WellKnownFeatureFlags.PrivateWallets] = false;
                    }
                }
            }
            // if feature flag is configured at all - set it to true
            else
            {
                featureFlags[WellKnownFeatureFlags.PrivateWallets] = true;
            }
            return Ok(featureFlags);
        }
    }
}
