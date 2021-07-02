using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Core.Services;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models._2Fa;
using LykkeApi2.Models.Whitelistings;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/whitelistings")]
    [ApiController]
    public class WhitelistingsController : Controller
    {
        private readonly Google2FaService _google2FaService;
        private readonly IRequestContext _requestContext;
        private readonly IAssetsHelper _assetsHelper;

        public WhitelistingsController(Google2FaService google2FaService, IRequestContext requestContext, IAssetsHelper assetsHelper)
        {
            _google2FaService = google2FaService;
            _requestContext = requestContext;
            _assetsHelper = assetsHelper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WhitelistingModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetWhitelistingsAsync()
        {
            return Ok(new List<WhitelistingModel>()
            {
                new WhitelistingModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = WhitelistingStatus.Pending,
                    AddressBase = Guid.NewGuid().ToString(),
                    AddressExtension = null,
                    CreatedAt = DateTime.UtcNow,
                    Name = Guid.NewGuid().ToString()
                },
                new WhitelistingModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = WhitelistingStatus.Active,
                    AddressBase = Guid.NewGuid().ToString(),
                    AddressExtension = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Name = Guid.NewGuid().ToString()
                }
            });
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(Google2FaResultModel<WhitelistingModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateWhitelistingAsync([FromBody] CreateWhitelistingRequest request)
        {
            var asset = await _assetsHelper.GetAssetAsync(request.AssetId);
            
            if(asset==null || asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);
            
            if(request.Code2Fa == "2222")
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressNotGenerated);
            
            if(request.Code2Fa == "3333")
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AddressAlreadyWhitelisted);
            
            var check2FaResult = await _google2FaService.Check2FaAsync<string>(_requestContext.ClientId, request.Code2Fa);

            if (check2FaResult != null)
                return Ok(check2FaResult);
            
            return Ok(Google2FaResultModel<WhitelistingModel>.Success(new WhitelistingModel
            {
                Id = Guid.NewGuid().ToString(),
                Status = WhitelistingStatus.Pending,
                AddressBase = request.AddressBase,
                AddressExtension = request.AddressExtension,
                CreatedAt = DateTime.UtcNow,
                Name = request.Name
            }));
        }
        
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Google2FaResultModel<string>), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteWhitelistingAsync([FromRoute] string id, [FromBody] DeleteWhitelistingRequest request)
        {
            var check2FaResult = await _google2FaService.Check2FaAsync<string>(_requestContext.ClientId, request.Code2Fa);

            if (check2FaResult != null)
                return Ok(check2FaResult);
            
            return Ok(Google2FaResultModel<string>.Success(id));
        }
    }
}
