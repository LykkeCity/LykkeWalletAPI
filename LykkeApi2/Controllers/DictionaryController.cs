using System.Net;
using System.Threading.Tasks;
using Lykke.Service.ClientDictionaries.AutorestClient.Models;
using Lykke.Service.ClientDictionaries.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Dictionary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/dictionary")]
    public class DictionaryController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IClientDictionariesClient _clientDictionariesClient;

        public DictionaryController(
            IRequestContext requestContext,
            IClientDictionariesClient clientDictionariesClient)
        {
            _requestContext = requestContext;
            _clientDictionariesClient = clientDictionariesClient;
        }
        
        [HttpGet("{key}")]
        [ProducesResponseType(typeof(DataModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetKey(string key)
        {
            if (!IsValidKey(key))
                return BadRequest();
            
            var response = await _clientDictionariesClient.GetAsync(_requestContext.ClientId, key);

            return response.Error == null ? Ok(new DataModel { Data = response.Data}) : Error(response.Error);
        }
        
        [HttpPost("{key}")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetKey(string key, [FromBody] DataModel request)
        {
            if (!IsValidKey(key) || !IsValidPayload(request?.Data))
                return BadRequest();
            
            var response = await _clientDictionariesClient.SetAsync(_requestContext.ClientId, key, request.Data);

            return response.Error == null ? Ok() : Error(response.Error);
        }

        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteKey(string key)
        {
            if (!IsValidKey(key))
                return BadRequest();
            
            var response = await _clientDictionariesClient.DeleteAsync(_requestContext.ClientId, key);

            return response.Error == null ? Ok() : Error(response.Error);
        }

        private IActionResult Error(ErrorModel error)
        {
            if (error.Type == ErrorType.NotFound)
                return NotFound();
            else
                return BadRequest();
        }

        private static bool IsValidKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key);
        }

        private static bool IsValidPayload(string payload)
        {
            return payload != null;
        }
    }
}