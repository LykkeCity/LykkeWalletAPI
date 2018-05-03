using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using LykkeApi2.Infrastructure;
using LykkeApi2.Infrastructure.Extensions;
using LykkeApi2.Models.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Swashbuckle.AspNetCore.Swagger;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsClient _operationsClient;
        private readonly IRequestContext _requestContext;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsService;

        public OperationsController(
            IOperationsClient operationsClient,
            IRequestContext requestContext,
            IExchangeOperationsServiceClient exchangeOperationsService)
        {
            _operationsClient = operationsClient;
            _requestContext = requestContext;
            _exchangeOperationsService = exchangeOperationsService;
        }

        /// <summary>
        /// Get operation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            OperationModel operation = null;

            try
            {
                operation = await _operationsClient.Get(id);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                throw;
            }

            if (operation == null)
                return NotFound();

            return Ok(operation.ToApiModel());
        }

        /// <summary>
        /// Create transfer operation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("transfer/{id}")]
        public async Task<IActionResult> Transfer([FromBody] CreateTransferRequest cmd, Guid id)
        {
            await _operationsClient.Transfer(id,
                new CreateTransferCommand
                {
                    ClientId = new Guid(_requestContext.ClientId),
                    Amount = cmd.Amount,
                    SourceWalletId =
                        cmd.SourceWalletId,
                    WalletId = cmd.WalletId,
                    AssetId = cmd.AssetId
                });

            return Created(Url.Action("Get", new {id}), id);
        }

        [HttpPost]
        [Route("payment/{id}/perform")]
        public async Task<IActionResult> PerformPayment(Guid id)
        {
            OperationModel operation;

            try
            {
                operation = await _operationsClient.Get(id);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                throw;
            }
            
            if (operation.Status != OperationStatus.Created)
            {
                if(operation.Status == OperationStatus.Completed)
                    return Forbid();
                
                return BadRequest();
            }
            
            var payemntContext = operation.ContextJson.DeserializeJson<PaymentContext>();

            try
            {
                var setOperationClientId =
                    await _operationsClient.SetPaymentClientId(id, new SetPaymenClientIdCommand
                    {
                        ClientId = Guid.Parse(_requestContext.ClientId)
                    });

                if (!setOperationClientId)
                    return BadRequest();
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.BadRequest)
                    return BadRequest();

                throw;
            }

            var result = await _exchangeOperationsService.TransferAsync(
                _requestContext.ClientId,
                payemntContext.WalletId.ToString(),
                (double) payemntContext.Amount,
                payemntContext.AssetId,
                "Common",
                null,
                null,
                id.ToString());

            if (result.IsOk())
                return Ok();

            if (result.IsDuplicate())
                return Forbid();
            
            return BadRequest();
        }

        /// <summary>
        /// Cancel operation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("cancel/{id}")]
        public async Task Cancel(Guid id)
        {
            await _operationsClient.Cancel(id);
        }
    }
}