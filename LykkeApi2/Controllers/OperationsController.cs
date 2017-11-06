using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Lykke.Service.Operations.Client.AutorestClient;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        private readonly IOperationsAPI _operationsApi;
        private readonly IRequestContext _requestContext;

        public OperationsController(IOperationsAPI operationsApi, IRequestContext requestContext)
        {
            _operationsApi = operationsApi;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get operation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [ApiExplorerSettings(GroupName = "Operations")]
        public async Task<IActionResult> Get(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "id is required" });

            var operation = await _operationsApi.ApiOperationsByIdGetAsync(id.Value);
            
            return Ok(operation);
        }

        /// <summary>
        /// Create transfer operation
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("transfer/{id}")]
        [ApiExplorerSettings(GroupName = "Operations")]       
        public async Task<IActionResult> Transfer([FromBody]CreateTransferCommand cmd, Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            await _operationsApi.ApiOperationsTransferByIdPostAsync(id.Value,
                new Lykke.Service.Operations.Client.AutorestClient.Models.CreateTransferCommand(
                    new Guid(_requestContext.ClientId), cmd.Amount, cmd.SourceWalletId, cmd.WalletId, cmd.AssetId));
            
            return Created(Url.Action("Get", new { id }), id);
        }

        /// <summary>
        /// Cancel operation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("cancel/{id}")]
        [ApiExplorerSettings(GroupName = "Operations")]
        public async Task<IActionResult> Cancel(Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            await _operationsApi.ApiOperationsCancelByIdPostAsync(id.Value);

            return Ok();
        }
    }
}