using System;
using System.Collections.Generic;
using System.Linq;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Core.Settings;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Infrastructure;
using Lykke.Service.CryptoIndex.Client.Api;
using Lykke.Service.CryptoIndex.Client.Models;

namespace LykkeApi2.Controllers
{
    [Route("api/cryptoIndices")]
    [ApiController]
    public class CryptoIndiciesController : Controller
    {
        private readonly CryptoIndexInstances _cryptoIndexInstances;
        private readonly Dictionary<string, IPublicApi> _clients = new Dictionary<string, IPublicApi>();

        public CryptoIndiciesController(
            CryptoIndexInstances cryptoIndexInstances)
        {
            _cryptoIndexInstances = cryptoIndexInstances;

            foreach (var instance in _cryptoIndexInstances.Instances)
            {
                var client = CreateApiClient(instance.ServiceUrl);
                _clients.Add(instance.DisplayName, client);
            }
        }

        /// <summary>
        /// Get crypto indices names.
        /// </summary>
        /// <returns></returns>
        [HttpGet("indicesNames")]
        [ProducesResponseType(typeof(ICollection<string>), (int)HttpStatusCode.OK)]
        public IActionResult GetCryptoIndicesNames()
        {
            var cryptoIndicesNames = _cryptoIndexInstances.Instances.Select(x => x.DisplayName).ToList();

            return Ok(cryptoIndicesNames);
        }

        /// <summary>
        /// Get last index history element.
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpGet("{indexName}/last")]
        [ProducesResponseType(typeof(PublicIndexHistory), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetLast(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetLastAsync();

            return Ok(result);
        }

        /// <summary>
        /// Get chart data for the last 24 hours by index name.
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpGet("{indexName}/history24h")]
        [ProducesResponseType(typeof(IDictionary<DateTime, decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetIndexHistory24H(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetIndexHistory24H();

            return Ok(result);
        }

        /// <summary>
        /// Get chart data for the last 5 days by index name.
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpGet("{indexName}/history5d")]
        [ProducesResponseType(typeof(IDictionary<DateTime, decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetIndexHistory5D(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetIndexHistory5D();

            return Ok(result);
        }

        /// <summary>
        /// Get chart data for the last 5 days by index name.
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpGet("{indexName}/history30d")]
        [ProducesResponseType(typeof(IDictionary<DateTime, decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetIndexHistory30D(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetIndexHistory30D();

            return Ok(result);
        }

        /// <summary>
        /// Get key numbers by index name.
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpGet("{indexName}/keyNumbers")]
        [ProducesResponseType(typeof(KeyNumbers), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetKeyNumbers(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetKeyNumbers();

            return Ok(result);
        }

        private IPublicApi CreateApiClient(string url)
        {
            var generator = HttpClientGenerator.BuildForUrl(url)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper())
                .WithoutRetries()
                .WithoutCaching()
                .Create();

            var client = generator.Generate<IPublicApi>();

            return client;
        }

    }
}
