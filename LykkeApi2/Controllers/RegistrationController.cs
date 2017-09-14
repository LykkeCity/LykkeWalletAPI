using Core.Messages;
using Core.Settings;
using FluentValidation.Validators;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Registration;
using Lykke.Service.Registration.Models;
using LykkeApi2.Credentials;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ValidationModels;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class RegistrationController : Controller
    {
        private readonly IVerifiedEmailsRepository _verifiedEmailsRepository;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILykkeRegistrationClient _lykkeRegistrationClient;
        private readonly ClientAccountLogic _clientAccountLogic;

        public RegistrationController(
            IVerifiedEmailsRepository verifiedEmailsRepository,
            ClientAccountLogic clientAccountLogic,
            IClientAccountClient clientAccountClient,
            ILykkeRegistrationClient lykkeRegistrationClient
                        )
        {
            _verifiedEmailsRepository = verifiedEmailsRepository;
            _lykkeRegistrationClient = lykkeRegistrationClient;
            _clientAccountClient = clientAccountClient;
            _clientAccountLogic = clientAccountLogic;
        }


        [HttpPost]
        public async Task<ResponseModel<AccountsRegistrationResponseModel>> Post([FromBody]AccountRegistrationModel model)
        {
            if (await _clientAccountLogic.IsTraderWithEmailExistsForPartnerAsync(model.Email, model.PartnerId))
            {
                return ResponseModel<AccountsRegistrationResponseModel>.CreateInvalidFieldError("email", Phrases.ClientWithEmailIsRegistered);
            }

            try
            {
                bool isIosDevice = this.IsIosDevice();

                var registrationModel = new RegistrationModel
                {
                    ClientInfo = model.ClientInfo,
                    UserAgent = this.GetUserAgent(),
                    ContactPhone = model.ContactPhone,
                    Email = model.Email,
                    FullName = model.FullName,
                    Hint = model.Hint,
                    PartnerId = model.PartnerId,
                    Password = model.Password,
                    Ip = this.GetIp(),
                    Changer = RecordChanger.Client,
                    IosVersion = isIosDevice ? this.GetVersion() : null,
                    Referer = HttpContext.Request.Host.Host
                };

                RegistrationResponse result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

                if (result == null)
                    return ResponseModel<AccountsRegistrationResponseModel>.CreateFail(
                            ResponseModel.ErrorCodeType.NoData, Phrases.TechnicalProblems);

                var resultPhone = await _clientAccountClient.InsertIndexedByPhoneAsync(
                                                            new IndexByPhoneRequestModel()
                                                            {
                                                                ClientId = result.Account.Id,
                                                                PhoneNumber = result.PersonalData.Phone,
                                                                PreviousPhoneNumber = null
                                                            });


                return ResponseModel<AccountsRegistrationResponseModel>.CreateOk(new AccountsRegistrationResponseModel
                {
                    Token = result.Token,
                    NotificationsId = result.NotificationsId,
                    PersonalData = new ApiPersonalDataModel
                    {
                        FullName = result.PersonalData.FullName,
                        Email = result.PersonalData.Email,
                        Phone = result.PersonalData.Phone
                    },
                    CanCashInViaBankCard = result.CanCashInViaBankCard,
                    SwiftDepositEnabled = result.SwiftDepositEnabled
                });
            }
            catch (Exception ex)
            {
                return ResponseModel<AccountsRegistrationResponseModel>.CreateInvalidFieldError("email", ex.StackTrace);
            }
        }
    }
}
