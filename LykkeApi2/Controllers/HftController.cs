using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.HftInternalService.Client.AutorestClient.Models;
using LykkeApi2.Models.ApiKey;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/hft")]
    public class HftController : Controller
    {
        private readonly IHftInternalServiceAPI _hftInternalService;

        public HftController(IHftInternalServiceAPI hftInternalService)
        {
            _hftInternalService = hftInternalService;
        }

        [HttpPost("createkey")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateKey([FromBody]CreateApiKeyRequest request)
        {
            var apiKey = await _hftInternalService.ApiKeysPostAsync(new CreateAccountRequest(request.ClientId));

            if (apiKey == null)
                return BadRequest();
            
            return Ok(new CreateApiKeyResponse { Key = apiKey.Key, Wallet = apiKey.Wallet });
        }

        [HttpDelete("deletekey/{key}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteKey(string key)
        {
            await _hftInternalService.ApiKeysByKeyDeleteAsync(key);

            return Ok();
        }
    }
}