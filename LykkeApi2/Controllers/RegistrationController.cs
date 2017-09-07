using Core.Messages;
using Core.Settings;
using FluentValidation.Validators;
using Lykke.Service.ClientAccount.Client;
using LykkeApi2.Models;
using LykkeApi2.Models.ClientAccountModels;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class RegistrationController : Controller
    {
        private readonly IVerifiedEmailsRepository _verifiedEmailsRepository;
        private readonly DeploymentSettings _deploymentSettings;
        private readonly IClientAccountClient _clientAccountClient;

        public RegistrationController(
            IVerifiedEmailsRepository verifiedEmailsRepository,
            DeploymentSettings deploymentSettings,
            IClientAccountClient clientAccountClient
            )
        {
            _verifiedEmailsRepository = verifiedEmailsRepository;
            _deploymentSettings = deploymentSettings;
            _clientAccountClient = clientAccountClient;
        }

        //[Authorize(Policy = LykkeConstants.MobilePhoneSecureAccess)]
        //[HttpGet]
        //public async Task<ResponseModel<GetRegistrationStatusResponseModel>> Get()
        //{
        //    var clientId = this.GetClientId();

        //    var kycStatusTask = _kycRepository.GetKycStatusAsync(clientId);
        //    var isPinEnteredTask = _pinSecurityRepository.IsPinEntered(clientId);
        //    var personalDataTask = _personalDataService.GetAsync(clientId);

        //    return ResponseModel<GetRegistrationStatusResponseModel>.CreateOk(
        //        new GetRegistrationStatusResponseModel
        //        {
        //            KycStatus = (await kycStatusTask).ConvertToApiModel(),
        //            PinIsEntered = await isPinEnteredTask,
        //            PersonalData = (await personalDataTask).ConvertToApiModel()
        //        });
        //}

        [HttpPost]
        public async Task<ResponseModel<AccountsRegistrationResponseModel>> Post([FromBody]AccountRegistrationModel model)
        {
            //var validEmail = await RegistrationValidationModel.ValidateEmail(model.Email, model.PartnerId,
            //    _deploymentSettings,
            //    _verifiedEmailsRepository);

            //if (!string.IsNullOrEmpty(validEmail))
            //{
            //    return ResponseModel<AccountsRegistrationResponseModel>.CreateInvalidFieldError("email", validEmail);
            //}        

            //if (_deploymentSettings.IsProduction && !await _verifiedEmailsRepository.IsEmailVerified(model.Email, model.PartnerId))
            //    return ResponseModel<AccountsRegistrationResponseModel>.CreateInvalidFieldError("email", Phrases.EmailNotVerified);

            if (!ModelState.IsValid)
            {
                //actionContext.Request.CreateErrorResponse(
                //HttpStatusCode.BadRequest, actionContext.ModelState)

                //var t = ModelState.Keys.ToList();
                //throw new Microsoft.Rest.ValidationException(ValidationRules.CannotBeNull, "email");
                //return ResponseModel<AccountsRegistrationResponseModel>.CreateModelStateInvalid(ModelState.ToString());

                return ResponseModel<AccountsRegistrationResponseModel>.CreateModelStateInvalid(BadRequest(ModelState).ToString());

                //ModelState.
                //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                //return ResponseModel<AccountsRegistrationResponseModel>.CreateModelStateInvalid("");
                //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);

            }
            else
            {
                return ResponseModel<AccountsRegistrationResponseModel>.CreateModelStateInvalid("");
            }


            //try
            //{
            //    bool isIosDevice = this.IsIosDevice();

            //    var registrationModel = new RegistrationModel
            //    {
            //        ClientInfo = model.ClientInfo,
            //        UserAgent = this.GetUserAgent(),
            //        ContactPhone = model.ContactPhone,
            //        Email = model.Email,
            //        FullName = model.FullName,
            //        Hint = model.Hint,
            //        PartnerId = model.PartnerId,
            //        Password = model.Password,
            //        Ip = this.GetIp(),
            //        Changer = RecordChanger.Client,
            //        IosVersion = isIosDevice ? this.GetVersion() : null,
            //        Referer = HttpContext.Request.Host.Host
            //    };

            //    RegistrationResponse result = await _lykkeRegistrationClient.RegisterAsync(registrationModel);

            //    if (result == null)
            //        return ResponseModel<AccountsRegistrationResponseModel>.CreateFail(
            //                ResponseModel.ErrorCodeType.NoData, Phrases.TechnicalProblems);

            //    await _clientAccountLogic.InsertIndexedByPhoneAsync(result.Account.Id, result.PersonalData.Phone, null);

            //    return ResponseModel<AccountsRegistrationResponseModel>.CreateOk(new AccountsRegistrationResponseModel
            //    {
            //        Token = result.Token,
            //        NotificationsId = result.NotificationsId,
            //        PersonalData = new ApiPersonalDataModel
            //        {
            //            FullName = result.PersonalData.FullName,
            //            Email = result.PersonalData.Email,
            //            Phone = result.PersonalData.Phone
            //        },
            //        CanCashInViaBankCard = result.CanCashInViaBankCard,
            //        SwiftDepositEnabled = result.SwiftDepositEnabled
            //    });
            //}
            //catch (Exception ex)
            //{
            //    return ResponseModel<AccountsRegistrationResponseModel>.CreateInvalidFieldError("email", ex.StackTrace);
            //}


        }

    }
}
