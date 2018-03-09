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
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetKey(string key)
        {
            var response = await _clientDictionariesClient.GetAsync(_requestContext.ClientId, key);

            return response.Error == null ? Ok(response.Data) : Error(response.Error);
        }
        
        [HttpPost("{key}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetKey(string key, [FromBody] SetKeyRequest request)
        {
            var response = await _clientDictionariesClient.SetAsync(_requestContext.ClientId, key, request.Data);

            return response.Error == null ? Ok() : Error(response.Error);
        }

        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteKey(string key)
        {
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
    }
}