using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsClient _operationsClient;
        private readonly IRequestContext _requestContext;

        public OperationsController(IOperationsClient operationsClient, IRequestContext requestContext)
        {
            _operationsClient = operationsClient;
            _requestContext = requestContext;
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
        public async Task<IActionResult> Transfer([FromBody]CreateTransferRequest cmd, Guid id)
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
            
            return Created(Url.Action("Get", new { id }), id);
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