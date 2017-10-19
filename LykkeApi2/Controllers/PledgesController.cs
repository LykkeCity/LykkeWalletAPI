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

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class PledgesController : Controller
    {
        private readonly IPledgesClient _pledgesClient;
        private readonly ILog _log;

        public PledgesController(ILog log, IPledgesClient pledgesClient)
        {
            _log = log ?? throw new ArgumentException(nameof(log));
            _pledgesClient = pledgesClient ?? throw new ArgumentException(nameof(pledgesClient));
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
        public async Task<ApiModels.CreatePledgeResponse> Create([FromBody] ApiModels.CreatePledgeRequest request)
        {
            var clientRequest = PledgesMapper.Instance.Map<ClientModels.CreatePledgeRequest>(request);
            var pledge = await _pledgesClient.Create(clientRequest);
            var response = PledgesMapper.Instance.Map<ApiModels.CreatePledgeResponse>(pledge);

            return response;
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
        public async Task<ApiModels.GetPledgeResponse> Get(string id)
        {
            var pledge = await _pledgesClient.Get(id);
            var response = PledgesMapper.Instance.Map<ApiModels.GetPledgeResponse>(pledge);

            return response;
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
        public async Task<ApiModels.UpdatePledgeResponse> Update(string id, [FromBody] ApiModels.UpdatePledgeRequest request)
        {
            var clientRequest = PledgesMapper.Instance.Map<ClientModels.UpdatePledgeRequest>(request);
            var pledge = await _pledgesClient.Update(id, clientRequest);
            var response = PledgesMapper.Instance.Map<ApiModels.UpdatePledgeResponse>(pledge);

            return response;
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
        public async Task Delete(string id)
        {
            await _pledgesClient.Delete(id);
        }

        /// <summary>
        /// Get pledges for provided client. 
        /// </summary>
        /// <param name="id">Id of the client we wanna get pledges for.</param>
        /// <returns></returns>
        [HttpGet("client/{id}")]
        [SwaggerOperation("GetPledgesByClientId")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IEnumerable<ApiModels.GetPledgeResponse>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<ApiModels.GetPledgeResponse>> GetPledgesByClientId(string id)
        {
            var pledges = await _pledgesClient.GetPledgesByClientId(id);
            var response = PledgesMapper.Instance.Map<IEnumerable<ApiModels.GetPledgeResponse>>(pledges);

            return response;
        }
    }
}