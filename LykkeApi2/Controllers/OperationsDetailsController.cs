using Common.Log;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.OperationsDetails;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.OperationsDetailsModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.SwaggerGen.Annotations;
using System;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{

    /// <summary>
    /// Controller is used for client details information logic
    /// </summary>
    [Authorize]
    [Route("api/operationsDetails")]
    public class OperationsDetailsController : Controller
    {
        private readonly ILog _log;
        private readonly IRequestContext _requestContext;
        private readonly IOperationDetailsInformationClient _operationDetailsInformationClient;

        public OperationsDetailsController(ILog log, IRequestContext requestContext,
                                           IOperationDetailsInformationClient operationDetailsInformationClient)
        {
            _log = log;
            _requestContext = requestContext;
            _operationDetailsInformationClient = operationDetailsInformationClient;
        }

        /// <summary>
        /// Inserts new operation details information data in database
        /// </summary>
        /// <param name="model">
        /// model: necessary values for inserting an operation details information data in database
        /// </param>
        /// <returns></returns>
        [HttpPost("create")]
        [SwaggerOperation("CreateOperationsDetails")]
        public async Task<IActionResult> CreateOperationsDetail([FromBody] OperationsDetailsModel model)
        {
            await _operationDetailsInformationClient.CreateAsync(new OperationDetailsInformation()
            {
                Id = Guid.NewGuid().ToString("N"),
                TransactionId = model.TransactionId,
                ClientId = _requestContext.ClientId,
                CreatedAt = DateTime.Now,
                Comment = model.Comment
            });

            return Ok();
        }

        /// <summary>
        /// Inserts new operation details information data in database
        /// </summary>
        /// <param name="model">
        /// model: necessary values for inserting an operation details information data in database
        /// </param>
        /// <returns>
        /// Returns the id of the new data from the database
        /// </returns>
        [HttpPost("register")]
        [SwaggerOperation("CreateOperationsDetails")]
        public async Task<IActionResult> RegisterOperationsDetail([FromBody] OperationsDetailsModel model)
        {
            var operationDetailsInfoId = await _operationDetailsInformationClient.RegisterAsync(new OperationDetailsInformation()
            {
                Id = Guid.NewGuid().ToString("N"),
                TransactionId = model.TransactionId,
                ClientId = _requestContext.ClientId,
                Comment = model.Comment
            });

            return Ok(operationDetailsInfoId);
        }
    }
}
