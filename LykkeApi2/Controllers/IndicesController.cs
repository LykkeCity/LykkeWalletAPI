using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.IndicesFacade.Client;
using Lykke.Service.IndicesFacade.Contract;
using LykkeApi2.Strings;

namespace LykkeApi2.Controllers
{
    [Route("api/indices")]
    [ApiController]
    public class IndicesController : Controller
    {
        private readonly IIndicesFacadeClient _indicesFacadeClient;

        public IndicesController(IIndicesFacadeClient indicesFacadeClient)
        {
            _indicesFacadeClient = indicesFacadeClient;
        }

        /// <summary>
        /// Returns all indices
        /// </summary>
        [HttpGet("")]
        [ProducesResponseType(typeof(IList<Index>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _indicesFacadeClient.Api.GetAllAsync();

            return Ok(result);
        }

        /// <summary>
        /// Returns index details by asset identifier
        /// </summary>
        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(Index), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return BadRequest(string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(assetId)));

            Index result;
            try
            {
                result = await _indicesFacadeClient.Api.GetAsync(assetId);
            }
            catch (ClientApiException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return Ok(result);
        }

        /// <summary>
        /// Returns history values by asset identifier and time interval
        /// </summary>
        [HttpGet("{assetId}/history/{timeInterval}")]
        [ProducesResponseType(typeof(IList<HistoryElement>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetHistoryAsync(string assetId, TimeInterval timeInterval)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return BadRequest(string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(assetId)));

            if (timeInterval == TimeInterval.Unspecified)
                return BadRequest(string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(timeInterval)));

            IList<HistoryElement> result;
            try
            {
                result = await _indicesFacadeClient.Api.GetHistoryAsync(assetId, timeInterval);
            }
            catch (ClientApiException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return Ok(result);
        }

        /// <summary>
        /// Returns raw prices from external exchanges for assets that are used for index calculation
        /// </summary>
        [HttpGet("{assetId}/prices")]
        [ProducesResponseType(typeof(AssetPrices[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetPricesAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return BadRequest(string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(assetId)));

            IList<AssetPrices> result;
            try
            {
                result = await _indicesFacadeClient.Api.GetPricesAsync(assetId);
            }
            catch (ClientApiException)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return Ok(result);
        }
    }
}
