using System.Collections.Generic;
using LykkeApi2.Models.Kyc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using Lykke.Common.ApiLibrary.Contract;
using Lykke.Common.ApiLibrary.Extensions;
using Lykke.Common.ApiLibrary.Validation;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Abstractions.Services.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using LykkeApiErrorResponse = Lykke.Common.ApiLibrary.Contract.LykkeApiErrorResponse;
using Lykke.Common.ApiLibrary.Exceptions;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/kycProfiles")]
    [ApiController]
    public class KycProfilesController : Controller
    {
        private readonly KycAdditionalDataValidator _kycAdditionalDataValidator;
        private readonly IKycProfileService _kycProfileService;
        private readonly IRequestContext _requestContext;

        private const string Changer = "LykkeApiv2";

        public KycProfilesController(
            IRequestContext requestContext, 
            IKycProfileService kycProfileService, 
            KycAdditionalDataValidator kycAdditionalDataValidator)
        {
            _requestContext = requestContext;
            _kycProfileService = kycProfileService;
            _kycAdditionalDataValidator = kycAdditionalDataValidator;
        }

        /// <summary>
        /// Posting additional personal data to pass KYC verification
        /// </summary>
        /// <param name="model">Additional data details</param>
        /// <response code="201">Additional data has been successfully posted</response>
        /// <response code="400"></response>
        [HttpPost]
        [Route("additionalPersonalInfo")]
        [SwaggerOperation("PostAdditionalPersonalInfo")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(LykkeApiErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> SubmitAdditionalPersonalInfo([FromBody] KycAdditionalInfoModel model)
        {
            var validationResult = await _kycAdditionalDataValidator.ValidateAsync(model);

            if (!validationResult.IsValid)
            {
                var modelState = new ModelStateDictionary();

                validationResult.AddToModelState(modelState, null);

                throw LykkeApiErrorException.BadRequest(
                    new LykkeApiErrorCode("InvalidInput", "One of the provided values was not valid."),
                    modelState.GetErrorMessage());
            }

            var changes = new KycPersonalDataChanges { Changer = Changer, Items = new Dictionary<string, JToken>() };

            changes.Items.Add(nameof(model.DateOfBirth), model.DateOfBirth);
            changes.Items.Add(nameof(model.Zip), model.Zip);
            changes.Items.Add(nameof(model.Address), model.Address);
            
            await _kycProfileService.UpdatePersonalDataAsync(_requestContext.ClientId, changes);

            return StatusCode((int) HttpStatusCode.Created);
        }
    }
}
