using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Core.Constants;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.Auth;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest.TransientFaultHandling;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly IRequestContext _requestContext;

        public AuthController(ILykkeRegistrationClient lykkeRegistrationClient, IRequestContext requestContext)
        {
            _lykkeRegistrationClient = lykkeRegistrationClient;
            _requestContext = requestContext;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]AuthRequestModel model)
        {   
            AuthResponse authResult = await _lykkeRegistrationClient.AuthorizeAsync(new AuthModel
            {
                ClientInfo = model.ClientInfo,
                Email = model.Email,
                Password = model.Password,
                Ip = _requestContext.GetIp(),
                UserAgent = _requestContext.GetUserAgent(),
                PartnerId = model.PartnerId
            });

            if (authResult.Status == AuthenticationStatus.Error)                
                return BadRequest(new {message = authResult.ErrorMessage});

            return Ok(new AuthResponseModel
            {
                AccessToken = authResult.Token,
            });
        }        
    }
}