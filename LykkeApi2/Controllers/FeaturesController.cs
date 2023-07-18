using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Repositories;
using LykkeApi2.Models.Features;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LykkeApi2.Controllers
{
    [Route("api/features")]
    [ApiController]
    public class FeaturesController : Controller
    {
        private readonly IFeaturesRepository _featuresRepository;

        public FeaturesController(IFeaturesRepository featuresRepository)
        {
            _featuresRepository = featuresRepository;
        }

        /// <summary>
        /// Get all features and their global enabled/disabled status.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(IDictionary<string, bool>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            return Ok(await _featuresRepository.GetAll(null));
        }
        
        /// <summary>
        /// Get all features and their enabled/disabled status for specific client.
        /// If there is no entry for specific client, global setting will be used.
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IDictionary<string, bool>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetForClient(string clientId)
        {
            return Ok(await _featuresRepository.GetAll(clientId));
        }
        
        /// <summary>
        /// Sets enabled/disabled status for a feature for a specific client
        /// If client id is not specified, then the feature will be toggled globally
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation("SetFeature")]
        public async Task<IActionResult> SetFeature([FromBody] ToggleFeatureRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FeatureName)) {
                return BadRequest("Feature name is required");
            }

            await _featuresRepository.AddOrUpdate(request.FeatureName, request.IsEnabled, request.ClientId);
            return Ok();
        }
    }
}
