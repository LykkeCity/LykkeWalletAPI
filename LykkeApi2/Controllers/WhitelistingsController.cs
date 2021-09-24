using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Core.Services;
using Google.Protobuf.WellKnownTypes;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Messages.Email;
using Lykke.Messages.Email.MessageData;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.TemplateFormatter.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models._2Fa;
using LykkeApi2.Models.Whitelistings;
using LykkeApi2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Asset;
using Swisschain.Sirius.Api.ApiContract.WhitelistItems;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/whitelistings")]
    [ApiController]
    public class WhitelistingsController : Controller
    {
        private readonly Google2FaService _google2FaService;
        private readonly IApiClient _siriusApiClient;
        private readonly IRequestContext _requestContext;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IPersonalDataService _personalDataService;
        private readonly IEmailSender _emailSender;
        private readonly ITemplateFormatter _templateFormatter;
        private readonly SiriusApiServiceClientSettings _siriusApiServiceClientSettings;
        private readonly WhitelistingSettings _whitelistingSettings;

        public WhitelistingsController(
            Google2FaService google2FaService,
            IRequestContext requestContext,
            IAssetsHelper assetsHelper,
            IApiClient siriusApiClient,
            IClientAccountClient clientAccountService,
            IPersonalDataService personalDataService,
            IEmailSender emailSender,
            ITemplateFormatter templateFormatter,
            SiriusApiServiceClientSettings siriusApiServiceClientSettings,
            WhitelistingSettings whitelistingSettings)
        {
            _google2FaService = google2FaService;
            _requestContext = requestContext;
            _assetsHelper = assetsHelper;
            _siriusApiClient = siriusApiClient;
            _clientAccountService = clientAccountService;
            _personalDataService = personalDataService;
            _emailSender = emailSender;
            _templateFormatter = templateFormatter;
            _siriusApiServiceClientSettings = siriusApiServiceClientSettings;
            _whitelistingSettings = whitelistingSettings;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WhitelistingResponseModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetWhitelistingsAsync()
        {
            var wallets = (await _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId, owner: OwnerType.Spot, walletType: WalletType.Trusted)).ToList();

            var result = new List<WhitelistItemResponse>();
            var noGeneratedAddresses = true;

            foreach (var wallet in wallets)
            {
                var siriusAccount = await TryGetSiriusAccountAsync(_requestContext.ClientId, wallet.Id);

                if (siriusAccount == null)
                    continue;

                noGeneratedAddresses = false;
                result.AddRange(await GetWhitelistItemsAsync(siriusAccount.Id));
            }

            if (noGeneratedAddresses)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressNotGenerated);

            var assets = await _assetsHelper.GetAllAssetsAsync();

            return Ok(result.Select(x => new WhitelistingResponseModel
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                WalletName = wallets.Single(y => y.Id == x.Scope.AccountReferenceId).Name,
                AssetName = assets.SingleOrDefault(y => y.SiriusAssetId == x.Details.AssetId)?.DisplayId,
                AddressBase = x.Details.Address,
                AddressExtension = x.Details.Tag,
                CreatedAt = x.CreatedAt.ToDateTime(),
                StartsAt = x.Lifespan.StartsAt.ToDateTime(),
                Status = x.Lifespan.StartsAt < Timestamp.FromDateTime(DateTime.UtcNow)
                    ? WhitelistingStatus.Active
                    : WhitelistingStatus.Pending
            }));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Google2FaResultModel<WhitelistingModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateWhitelistingAsync([FromBody] CreateWhitelistingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AddressBase))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var check2FaResult = await _google2FaService.Check2FaAsync<string>(_requestContext.ClientId, request.Code2Fa);

            if (check2FaResult != null)
                return Ok(check2FaResult);

            var asset = await _assetsHelper.GetAssetAsync(request.AssetId);
            var assetsAvailableToUser = await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);

            if (asset == null || asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius &&
                assetsAvailableToUser.Contains(asset.Id))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            var wallets = (await _clientAccountService.Wallets.GetClientWalletsFilteredAsync(_requestContext.ClientId,
                owner: OwnerType.Spot, walletType: WalletType.Trusted)).ToList();

            if (wallets.All(x => x.Id != request.WalletId))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput);

            var siriusAssetResponse = await _siriusApiClient.Assets.SearchAsync(new AssetSearchRequest { Id = asset.SiriusAssetId });
            var siriusAsset = siriusAssetResponse.Body.Items.FirstOrDefault();

            if (siriusAsset == null)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AssetUnavailable);

            var siriusAccount = await TryGetSiriusAccountAsync(_requestContext.ClientId, request.WalletId);

            if (siriusAccount == null)
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.BlockchainWalletDepositAddressNotGenerated);

            var whitelistedItems = await GetWhitelistItemsAsync(siriusAccount.Id);

            if (whitelistedItems.Any(x => x.Details.Address == request.AddressBase && x.Details.Tag == request.AddressExtension))
                throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.AddressAlreadyWhitelisted);

            var requestId = $"{request.WalletId}:{request.Name}:{request.AssetId}:{request.AddressBase}:{request.AddressExtension ?? string.Empty}";

            var result = await _siriusApiClient.WhitelistItems.CreateAsync(new WhitelistItemCreateRequest
            {
                Name = request.Name,
                Scope = new WhitelistItemScope
                {
                    BrokerAccountId = _siriusApiServiceClientSettings.BrokerAccountId, AccountId = siriusAccount.Id
                },
                Details = new WhitelistItemDetails
                {
                    AssetId = siriusAsset.Id,
                    BlockchainId = siriusAsset.BlockchainId,
                    Address = request.AddressBase,
                    Tag = request.AddressExtension,
                    TagType =
                        new NullableWhitelistItemTagType
                        {
                            TagType = WhitelistItemTagType.Number //TODO: specify tag type depending on the blockchain
                        },
                    TransactionType = WhitelistTransactionType.Any
                },
                Lifespan = new WhitelistItemLifespan
                {
                    StartsAt = Timestamp.FromDateTime(DateTime.UtcNow.Add(_whitelistingSettings.WaitingPeriod))
                },
                RequestId = requestId
            });

            if (result.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.WhitelistItem)
            {
                await SendWhitelistEmail(_requestContext.ClientId, asset.DisplayId, request.AddressBase,
                    request.AddressExtension);

                return Ok(Google2FaResultModel<WhitelistingResponseModel>.Success(new WhitelistingResponseModel
                {
                    Id = result.WhitelistItem.Id.ToString(),
                    WalletName = wallets.Single(y => y.Id == request.WalletId).Name,
                    AssetName = asset.DisplayId,
                    Status = WhitelistingStatus.Pending,
                    AddressBase = request.AddressBase,
                    AddressExtension = request.AddressExtension,
                    StartsAt = result.WhitelistItem.Lifespan.StartsAt.ToDateTime(),
                    CreatedAt = result.WhitelistItem.CreatedAt.ToDateTime(),
                    Name = request.Name
                }));
            }

            throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.WhitelistingError);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Google2FaResultModel<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteWhitelistingAsync([FromRoute] long id,
            [FromBody] DeleteWhitelistingRequest request)
        {
            var check2FaResult = await _google2FaService.Check2FaAsync<string>(_requestContext.ClientId, request.Code2Fa);

            if (check2FaResult != null)
                return Ok(check2FaResult);

            var requestId = $"{id}";

            await _siriusApiClient.WhitelistItems.DeleteAsync(new WhitelistItemDeleteRequest
            {
                Id = id,
                RequestId = requestId
            });

            return Ok(Google2FaResultModel<string>.Success(id.ToString()));
        }

        private async Task<AccountResponse> TryGetSiriusAccountAsync(string clientId, string walletId)
        {
            var accountSearchResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _siriusApiServiceClientSettings.BrokerAccountId,
                UserNativeId = clientId,
                ReferenceId = walletId
            });

            if (accountSearchResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                throw new Exception("Error fetching Sirius Account");
            }

            return accountSearchResponse.Body.Items.SingleOrDefault();
        }

        private async Task<IEnumerable<WhitelistItemResponse>> GetWhitelistItemsAsync(long accountId)
        {
            var whitelistedItems = await _siriusApiClient.WhitelistItems.SearchAsync(new WhitelistItemSearchRequest
            {
                BrokerAccountId = _siriusApiServiceClientSettings.BrokerAccountId,
                AccountId = accountId,
                IsRemoved = false
            });

            if (whitelistedItems.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error)
            {
                throw new Exception(whitelistedItems.Error.ErrorMessage);
            }

            return whitelistedItems.WhitelistItems.Items;
        }

        private async Task SendWhitelistEmail(string clientId, string asset, string address, string tag)
        {
            var clientAccountTask = _clientAccountService.ClientAccountInformation.GetByIdAsync(clientId);
            var pdTask = _personalDataService.GetAsync(clientId);

            await Task.WhenAll(clientAccountTask, pdTask);

            var clientAccount = clientAccountTask.Result;
            var pd = pdTask.Result;

            var template = await _templateFormatter.FormatAsync("AddressWhitelistedTemplate", clientAccount.PartnerId,
                "EN", new { FullName = pd.FullName, Asset = asset, Address = address, Tag = tag ?? "---", Year = DateTime.UtcNow.Year });

            var msgData = new PlainTextData
            {
                Sender = pd.Email,
                Subject = template.Subject,
                Text = template.HtmlBody
            };

            await _emailSender.SendEmailAsync(clientAccount.PartnerId, pd.Email, msgData);
        }
    }
}
