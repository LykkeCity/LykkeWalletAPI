using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Blockchain;
using Core.Constants;
using Core.Repositories;
using Core.Services;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Cqrs;
using Lykke.Job.HistoryExportBuilder.Contract;
using Lykke.Job.HistoryExportBuilder.Contract.Commands;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.History.Client;
using Lykke.Service.History.Contracts.Enums;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Blockchain;
using LykkeApi2.Models.History;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ErrorResponse = LykkeApi2.Models.ErrorResponse;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LkkInvestmentRequestController : Controller
    {
        private readonly ILkkInvestmentRequestRepository _lkkInvestmentRequestRepository;
        private readonly IRequestContext _requestContext;

        public LkkInvestmentRequestController(
            ILkkInvestmentRequestRepository lkkInvestmentRequestRepository,
            IRequestContext requestContext)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _lkkInvestmentRequestRepository = lkkInvestmentRequestRepository;
        }

        /// <summary>
        /// Handles LKK investment requests
        /// </summary>

        /// <param name="model">
        /// Amount - amount of LKK which client is willing to buy
        /// PurchaseOption - the way in which it should be processed
        /// </param>
        /// <returns></returns>
        /// <exception cref="LykkeApiErrorException"></exception>
        [HttpPost]
        [SwaggerOperation("Send")]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Send(
            [FromBody] LkkInvestmentRequestModel model)
        {
            if (string.IsNullOrEmpty(model.Amount))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput, $"{nameof(model.Amount)} should not null");

            if (string.IsNullOrEmpty(model.PurchaseOption))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput, $"{nameof(model.PurchaseOption)} should not null");

            var clientId = _requestContext.ClientId;

            var requestId = Guid.NewGuid().ToString();
                
            await _lkkInvestmentRequestRepository.Add(clientId, requestId, model.Amount, model.PurchaseOption);
            
            return Ok();
        }
    }

    public class LkkInvestmentRequestModel
    {
        public string Amount { get; set; }
        public string PurchaseOption { get; set; }
    }
}
