using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Lykke.Common.ApiLibrary.Contract;
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
        /// Returns index details by index asset identifier
        /// </summary>
        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(Index), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                throw FieldShouldNotBeEmpty(nameof(assetId));

            Index result;

            try
            {
                result = await _indicesFacadeClient.Api.GetAsync(assetId);
            }
            catch
            {
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            }

            return Ok(result);
        }

        /// <summary>
        /// Returns history values by index asset identifier and time interval
        /// </summary>
        [HttpGet("{assetId}/history/{timeInterval}")]
        [ProducesResponseType(typeof(IList<HistoryElement>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetHistoryAsync(string assetId, TimeInterval timeInterval)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                throw FieldShouldNotBeEmpty(nameof(assetId));

            if (timeInterval == TimeInterval.Unspecified)
                throw FieldShouldBeSpecified(nameof(timeInterval));

            IList<HistoryElement> result;

            try
            {
                result = await _indicesFacadeClient.Api.GetHistoryAsync(assetId, timeInterval);
            }
            catch
            {
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
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
                throw FieldShouldNotBeEmpty(nameof(assetId));

            IList<AssetPrices> result;

            try
            {
                result = await _indicesFacadeClient.Api.GetPricesAsync(assetId);
            }
            catch
            {
                throw LykkeApiErrorException.NotFound(LykkeApiErrorCodes.Service.AssetNotFound);
            }

            return Ok(result);
        }

        private LykkeApiErrorException FieldShouldNotBeEmpty(string argumentName)
        {
            return LykkeApiErrorException.BadRequest(new LykkeApiErrorCode(
                ((int)HttpStatusCode.BadRequest).ToString(),
                string.Format(Phrases.FieldShouldNotBeEmptyFormat, argumentName)));
        }

        private LykkeApiErrorException FieldShouldBeSpecified(string argumentName)
        {
            return LykkeApiErrorException.BadRequest(new LykkeApiErrorCode(
                ((int)HttpStatusCode.BadRequest).ToString(),
                string.Format($"Field {argumentName} should be specified")));
        }
    }
}
