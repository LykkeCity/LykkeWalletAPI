    using Common;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Models;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AccountExistController : Controller
    {
        private readonly IClientAccountClient _clientAccountClient;

        public AccountExistController(IClientAccountClient clientAccountClient)
        {
            _clientAccountClient = clientAccountClient;
        }

        [HttpGet]
        public async Task<ResponseModel<Models.ClientAccountModels.AccountExistResultModel>> Get([FromQuery]string email)
        {
            if (string.IsNullOrEmpty(email))
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", Phrases.FieldShouldNotBeEmpty);

            email = email.ToLower();

            if (!email.IsValidEmail())
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", Phrases.InvalidAddress);

            var result = false;
            try
            {
                result = await _clientAccountClient.CheckIfAccountExists(email);

                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateOk(
               new Models.ClientAccountModels.AccountExistResultModel { IsEmailRegistered = result });
            }
            catch (Exception ex)
            {
                return ResponseModel<Models.ClientAccountModels.AccountExistResultModel>.CreateInvalidFieldError("email", ex.Message);
            }
        }
    }
}
