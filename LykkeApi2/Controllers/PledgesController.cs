using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Common.Log;
using Lykke.Service.Pledges.Client;
using Swashbuckle.SwaggerGen.Annotations;
using System.Net;
using ApiModels = LykkeApi2.Models.Pledges;
using LykkeApi2.Mappers;
using ClientModels = Lykke.Service.Pledges.Client.AutorestClient.Models;
using Microsoft.AspNetCore.Authorization;
using LykkeApi2.Infrastructure;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/pledges")]
    public class PledgesController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IPledgesClient _pledgesClient;
        private readonly ILog _log;

        public PledgesController(ILog log, IPledgesClient pledgesClient, IRequestContext requestContext)
        {
            _log = log ?? throw new ArgumentException(nameof(log));
            _pledgesClient = pledgesClient ?? throw new ArgumentException(nameof(pledgesClient));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        }

        /// <summary>
        /// Create a new pledge.
        /// </summary>
        /// <param name="request">Pledge value.</param>
        /// <returns>Created pledge.</returns>
        [HttpPost]
        [SwaggerOperation("CreatePledge")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiModels.CreatePledgeResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Create([FromBody] ApiModels.CreatePledgeRequest request)
        {
            if (request == null)
                return BadRequest();

            var clientRequest = PledgesMapper.Instance.Map<ClientModels.CreatePledgeRequest>(request);
            clientRequest.ClientId = _requestContext.ClientId;

            var pledge = await _pledgesClient.Create(clientRequest);
            var response = PledgesMapper.Instance.Map<ApiModels.CreatePledgeResponse>(pledge);

            return Ok(response);
        }

        /// <summary>
        /// Get pledge.
        /// </summary>
        /// <param name="id">Id of the pledge we wanna find.</param>
        /// <returns>Found pledge.</returns>
        [HttpGet("{id}")]
        [SwaggerOperation("GetPledge")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiModels.GetPledgeResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(string id)
        {
            if (String.IsNullOrEmpty(id))
                return BadRequest();

            var pledge = await _pledgesClient.Get(id);

            if (pledge == null)
                return NotFound();

            var response = PledgesMapper.Instance.Map<ApiModels.GetPledgeResponse>(pledge);

            return Ok(response);
        }

        /// <summary>
        /// Update pledge details.
        /// </summary>
        /// <param name="id">Id of the pledge we wanna update.</param>
        /// <param name="request">Pledge values we wanna change.</param>
        /// <returns>Updated pledge.</returns>
        [HttpPut("{id}")]
        [SwaggerOperation("UpdatePledge")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiModels.UpdatePledgeResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(string id, [FromBody] ApiModels.UpdatePledgeRequest request)
        {
            if (String.IsNullOrEmpty(id) || request == null)
                return BadRequest();

            var clientRequest = PledgesMapper.Instance.Map<ClientModels.UpdatePledgeRequest>(request);
            clientRequest.ClientId = _requestContext.ClientId;

            var pledge = await _pledgesClient.Update(id, clientRequest);
            var response = PledgesMapper.Instance.Map<ApiModels.UpdatePledgeResponse>(pledge);

            return Ok(response);
        }

        /// <summary>
        /// Delete pledge.
        /// </summary>
        /// <param name="id">Id of the pledge we wanna delete.</param>
        [HttpDelete("{id}")]
        [SwaggerOperation("DeletePledge")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(string id)
        {
            if (String.IsNullOrEmpty(id))
                return BadRequest();

            await _pledgesClient.Delete(id);

            return Ok();
        }

        /// <summary>
        /// Get all client pledges. 
        /// </summary>
        [HttpGet]
        [SwaggerOperation("GetPledges")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<ApiModels.GetPledgeResponse>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPledges()
        {
            var pledges = await _pledgesClient.GetPledgesByClientId(_requestContext.ClientId);
            var response = PledgesMapper.Instance.Map<IEnumerable<ApiModels.GetPledgeResponse>>(pledges);

            return Ok(response);
        }
    }
}