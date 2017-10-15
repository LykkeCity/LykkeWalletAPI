using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.HftInternalService.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ApiKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using CreateApiKeyRequest = LykkeApi2.Models.ApiKey.CreateApiKeyRequest;

namespace LykkeApi2.Controllers
{
    [Route("api/hft")]
    [Authorize]
    public class HftController : Controller
    {
        private readonly IHftInternalServiceAPI _hftInternalService;
        private readonly IRequestContext _requestContext;

        public HftController(IHftInternalServiceAPI hftInternalService, IRequestContext requestContext)
        {
            _hftInternalService = hftInternalService ?? throw new ArgumentNullException(nameof(hftInternalService));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        }

        /// <summary>
        /// Create trusted wallet and generate API-key.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("key")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateKey([FromBody] CreateApiKeyRequest request)
        {
            var apiKey = await _hftInternalService.CreateKeyAsync(
                new Lykke.Service.HftInternalService.Client.AutorestClient.Models.CreateApiKeyRequest(_requestContext.ClientId, request.Name));

            if (apiKey == null)
                return BadRequest();

            return Ok(new CreateApiKeyResponse { ApiKey = apiKey.Key, WalletId = apiKey.Wallet });
        }

        /// <summary>
        /// Create new api-key for existing wallet.
        /// </summary>
        /// <param name="walletId"></param>
        /// <returns></returns>
        [HttpPut("{walletId}/regenerateKey")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [SwaggerOperation("RegenerateKey")]
        public async Task<IActionResult> RegenerateKey(string walletId)
        {
            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            var apiKey = clientKeys.FirstOrDefault(x => x.Wallet == walletId);
            if (apiKey != null)
            {
                await _hftInternalService.RegenerateKeyAsync(new RegenerateKeyRequest { ClientId = _requestContext.ClientId, WalletId = apiKey.Wallet });
                return Ok();
            }
            return NotFound();
        }

        /// <summary>
        /// Delete API-key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteKey(string key)
        {
            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            if (clientKeys.Any(x => x.Key == key))
            {
                await _hftInternalService.DeleteKeyAsync(key);
                return Ok();
            }
            return NotFound();
        }
    }
}