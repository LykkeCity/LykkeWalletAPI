using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.HftInternalService.Client.Keys;
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
        private readonly IHftInternalClient _hftInternalService;
        private readonly IRequestContext _requestContext;
        private readonly Google2FaService _google2FaService;

        public HftController(
            IHftInternalClient hftInternalService,
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
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("regenerateKey")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Google2FaResultModel<CreateApiKeyResponse>), (int)HttpStatusCode.NotFound)]
        [SwaggerOperation("RegenerateKey")]
        public async Task<IActionResult> RegenerateKey([FromBody]RegenerateKeyRequestModel request)
        {
            var check2FaResult = await _google2FaService.Check2FaAsync<CreateApiKeyResponse>(_requestContext.ClientId, request.Code);

            if (check2FaResult != null)
                return Ok(check2FaResult);

            var clientKeys = await _hftInternalService.Keys.GetKeys(_requestContext.ClientId);
            var existingApiKey = clientKeys.FirstOrDefault(x => x.WalletId == request.Id);

            if (existingApiKey != null)
            {
                var apiKey = await _hftInternalService.Keys.UpdateKey(new UpdateApiKeyModel
                {
                    ClientId = _requestContext.ClientId,
                    WalletId = existingApiKey.WalletId,
                    Apiv2Only = request.Apiv2Only
                });

                var result = new CreateApiKeyResponse {ApiKey = apiKey.ApiKey, WalletId = apiKey.WalletId, Apiv2Only = request.Apiv2Only};
                return Ok(Google2FaResultModel<CreateApiKeyResponse>.Success(result));
            }

            return NotFound();
        }
    }
}
