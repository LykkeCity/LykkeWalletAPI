using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CryptoIndicesController : Controller
    {
        private readonly CryptoIndexInstances _cryptoIndexInstances;
        private readonly Dictionary<string, IPublicApi> _clients = new Dictionary<string, IPublicApi>();

        public CryptoIndicesController(
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
        /// <returns>All crypto indices names</returns>
        [HttpGet("indicesNames")]
        [ProducesResponseType(typeof(ICollection<string>), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public IActionResult GetCryptoIndicesNames()
        {
            var cryptoIndicesNames = _cryptoIndexInstances.Instances.Select(x => x.DisplayName).ToList();

            return Ok(cryptoIndicesNames);
        }

        /// <summary>
        /// Get last index details by index name.
        /// </summary>
        /// <param name="indexName">Name of the index</param>
        /// <returns>Details about last index calculation</returns>
        [HttpGet("{indexName}/last")]
        [ProducesResponseType(typeof(PublicIndexHistory), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
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
        /// Get chart data by index name and time interval.
        /// </summary>
        /// <param name="indexName">Name of the index</param>
        /// <param name="timeInterval">Time interval</param>
        /// <returns>Set of points for line chart</returns>
        [HttpGet("{indexName}/history")]
        [ProducesResponseType(typeof(IDictionary<DateTime, decimal>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetIndexHistory(string indexName, TimeInterval timeInterval)
        {
            if (string.IsNullOrWhiteSpace(indexName) || timeInterval == TimeInterval.Unspecified)
                return BadRequest();

            if (!_clients.Keys.Contains(indexName))
                return NotFound();

            var publicApi = _clients[indexName];

            var result = await publicApi.GetIndexHistory(timeInterval);

            return Ok(result);
        }

        /// <summary>
        /// Get key numbers by index name.
        /// </summary>
        /// <param name="indexName">Name of the index</param>
        /// <returns>Key numbers</returns>
        [HttpGet("{indexName}/keyNumbers")]
        [ProducesResponseType(typeof(KeyNumbers), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
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
