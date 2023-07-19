using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Repositories;
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

        public FeaturesController(IFeaturesRepository featuresRepository, IRequestContext requestContext)
        {
            _featuresRepository = featuresRepository;
            _requestContext = requestContext;
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
            return Ok(await _featuresRepository.GetAll(clientId));
        }
    }
}
