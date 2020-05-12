using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.HftInternalService.Client.AutorestClient;
using Lykke.Service.HftInternalService.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models._2Fa;
using LykkeApi2.Models.ApiKey;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/hft")]
    [ApiController]
    public class HftController : Controller
    {
        private readonly IHftInternalServiceAPI _hftInternalService;
        private readonly IRequestContext _requestContext;
        private readonly Google2FaService _google2FaService;

        public HftController(
            IHftInternalServiceAPI hftInternalService,
            IRequestContext requestContext,
            Google2FaService google2FaService
            )
        {
            _hftInternalService = hftInternalService ?? throw new ArgumentNullException(nameof(hftInternalService));
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _google2FaService = google2FaService;
        }

        /// <summary>
        /// Create new api-key for existing wallet.
        /// </summary>
        /// <param name="walletId">wallet Id</param>
        /// <param name="walletId">2Fa code</param>
        /// <returns></returns>
        [HttpPut("{walletId}/{code}/regenerateKey")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Google2FaResultModel<CreateApiKeyResponse>), (int)HttpStatusCode.NotFound)]
        [SwaggerOperation("RegenerateKey")]
        public async Task<IActionResult> RegenerateKey(string walletId, string code)
        {
            var check2FaResult = await _google2FaService.Check2FaAsync<CreateApiKeyResponse>(_requestContext.ClientId, code);

            if (check2FaResult != null)
                return Ok(check2FaResult);

            var clientKeys = await _hftInternalService.GetKeysAsync(_requestContext.ClientId);
            var existingApiKey = clientKeys.FirstOrDefault(x => x.Wallet == walletId);

            if (existingApiKey != null)
            {
                var apiKey = await _hftInternalService.RegenerateKeyAsync(new RegenerateKeyRequest { ClientId = _requestContext.ClientId, WalletId = existingApiKey.Wallet });
                var result = new CreateApiKeyResponse {ApiKey = apiKey.Key, WalletId = apiKey.Wallet};
                return Ok(Google2FaResultModel<CreateApiKeyResponse>.Success(result));
            }

            return NotFound();
        }
    }
}
