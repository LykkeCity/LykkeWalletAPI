using System;
using System.Threading.Tasks;
using LykkeApi2.Models.Operations;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/operations")]
    public class OperationsController : Controller
    {
        [HttpGet]
        [Route("{id}")]
        [ApiExplorerSettings(GroupName = "Operations")]
        public IActionResult Get(Guid? id)
        {
            var model = new
            {
                Type = "Transfer",
                Status = "Created",
                ClientId = Guid.NewGuid(),
                Context = new
                {
                    AssetId = "USD",
                    Amount = 100.50d,
                    WalletId = Guid.NewGuid()
                }
            };

            return Ok(model);
        }

        [HttpPost]
        [Route("transfer/{id}")]
        [ApiExplorerSettings(GroupName = "Operations")]
        public async Task<IActionResult> Post([FromBody]CreateTransferCommand cmd, Guid? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Operation id is required" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            return Created(Url.Action("Get", new { id }), id);
        }
    }
}