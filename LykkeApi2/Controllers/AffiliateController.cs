using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Affiliate.Client;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Route("api/affiliate")]
    public class AffiliateController : Controller
    {
        private readonly IAffiliateClient _affiliateClient;
        private readonly IRequestContext _requestContext;

        public AffiliateController(IAffiliateClient affiliateClient,IRequestContext requestContext)
        {
            this._affiliateClient = affiliateClient;
            this._requestContext = requestContext;
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> Get()
        {
            //var val = await _affiliateClient.Get();
            return Ok();
        }
    }
}