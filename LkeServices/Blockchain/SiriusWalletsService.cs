using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Blockchain;
using Google.Protobuf.WellKnownTypes;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Address;
using Swisschain.Sirius.Api.ApiContract.Asset;
using Swisschain.Sirius.Api.ApiContract.Blockchain;
using Swisschain.Sirius.Api.ApiContract.Common;
using Swisschain.Sirius.Api.ApiContract.User;
using Swisschain.Sirius.Api.ApiContract.WhitelistItems;

namespace LkeServices.Blockchain
{
    public class SiriusWalletsService : ISiriusWalletsService
    {
        private readonly long _brokerAccountId;
        private readonly int _retryCount;
        private readonly TimeSpan _retryTimeout;
        private readonly IApiClient _siriusApiClient;
        private readonly ILog _log;
        private readonly IEnumerable<TimeSpan> _delay;

        public SiriusWalletsService(
            long brokerAccountId,
            int retryCount,
            TimeSpan retryTimeout,
            IApiClient siriusApiClient,
            ILog log)
        {
            _brokerAccountId = brokerAccountId;
            _retryCount = retryCount;
            _retryTimeout = retryTimeout;
            _siriusApiClient = siriusApiClient;
            _log = log;

            _delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 7, fastFirst: true);
        }

        public async Task CreateWalletsAsync(string clientId)
        {
            long? accountId = null;

            var accountResponse = await SearchAccountAsync(clientId);

            if (accountResponse == null)
            {
                _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error getting account from sirius", context: new { clientId });
                return;
            }

            if (accountResponse.Body.Items.Count == 0)
            {
                string accountRequestId = $"{_brokerAccountId}_{clientId}_account";
                string userRequestId = $"{clientId}_user";

                var userCreateResponse = await CreateUserAsync(clientId, userRequestId);

                if (userCreateResponse == null)
                {
                    _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating user in sirius",
                        context: new { clientId, requestId = userRequestId });
                    return;
                }

                var createResponse = await CreateAccountAsync(clientId, userCreateResponse.User.Id, accountRequestId);

                if (createResponse == null)
                {
                    _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating account in sirius",
                        context: new { clientId, requestId = accountRequestId });
                    return;
                }

                accountId = createResponse.Body.Account.Id;

                await WaitForActiveWalletsAsync(clientId, accountId);
            }
            else
            {
                accountId = accountResponse.Body.Items.FirstOrDefault()?.Id;
            }

            var whitelistItemRequest = new WhitelistItemCreateRequest
            {
                Name = "Trading Wallet Whitelist",
                Scope = new WhitelistItemScope { BrokerAccountId = _brokerAccountId, AccountId = accountId, UserNativeId = clientId },
                Details = new WhitelistItemDetails
                {
                    TransactionType = WhitelistTransactionType.Any, TagType = new NullableWhitelistItemTagType { Null = NullValue.NullValue }
                },
                Lifespan = new WhitelistItemLifespan { StartsAt = Timestamp.FromDateTime(DateTime.UtcNow) },
                RequestId = $"lykke:trading_wallet:{clientId}"
            };

            var whitelistItemCreateResponse = await CreateWhitelistItemAsync(whitelistItemRequest);

            if (whitelistItemCreateResponse == null)
            {
                _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating Whitelist item",
                    context: new { clientId, accountId });
            }
        }

        public async Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId)
        {
            var accountSearchResponse = await SearchAccountAsync(clientId);

            if (accountSearchResponse == null)
            {
                _log.WriteWarning(nameof(GetWalletAdderssAsync), info: "Error getting account from sirius", context: new { clientId });
                return null;
            }

            var accountId = accountSearchResponse.Body.Items.FirstOrDefault()?.Id;

            if (accountId == null)
                return null;

            var searchResponse = await SearchAccountDetailsAsync(clientId, accountId.Value, assetId);

            if (searchResponse != null)
                return searchResponse.Body.Items.FirstOrDefault();

            _log.WriteWarning(nameof(GetWalletAdderssAsync), info: "Error getting account details from sirius",
                context: new { accountId, clientId, assetId });

            return null;
        }

        public async Task<bool> IsAddressValidAsync(string blockchainId, string address)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(IsAddressValidAsync), new { blockchainId, address }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<AddressIsValidResponse>(response =>
                    {
                        if (response.ResultCase == AddressIsValidResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(IsAddressValidAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == AddressIsValidResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () =>
                    await _siriusApiClient.Addresses.IsValidAsync(new AddressIsValidRequest
                        {
                            Address = address, BlockchainId = blockchainId
                        }
                    ));

                if (result.ResultCase != AddressIsValidResponse.ResultOneofCase.Error)
                    return result.Body.IsFormatValid;

                _log.WriteError(nameof(IsAddressValidAsync), new { blockchainId, address });
                return false;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(IsAddressValidAsync), new { blockchainId, address }, ex);
                return false;
            }
        }

        public async Task<List<BlockchainResponse>> GetBlockchainsAsync()
        {
            var emptyResult = new List<BlockchainResponse>();

            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(GetBlockchainsAsync), null, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<BlockchainSearchResponse>(response =>
                    {
                        if (response.ResultCase == BlockchainSearchResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(GetBlockchainsAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == BlockchainSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Blockchains.SearchAsync(new BlockchainSearchRequest()));

                if (result.ResultCase != BlockchainSearchResponse.ResultOneofCase.Error)
                    return result.Body.Items.ToList();

                _log.WriteError(nameof(GetBlockchainsAsync), null);
                return emptyResult;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(GetBlockchainsAsync), null, ex);
                return emptyResult;
            }
        }

        public async Task<AccountSearchResponse> SearchAccountAsync(string clientId, string walletId = null)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(SearchAccountAsync), new { clientId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<AccountSearchResponse>(response =>
                    {
                        if (response.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(SearchAccountAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == AccountSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
                {
                    BrokerAccountId = _brokerAccountId, UserNativeId = clientId, ReferenceId = walletId ?? clientId
                }));

                if (result.ResultCase != AccountSearchResponse.ResultOneofCase.Error)
                    return result;

                _log.WriteError(nameof(SearchAccountAsync), new { clientId });
                return null;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(SearchAccountAsync), new { clientId }, ex);
                return null;
            }
        }

        public async Task<AssetResponse> GetAssetByIdAsync(long assetId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(GetAssetByIdAsync), new { assetId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<AssetSearchResponse>(response =>
                    {
                        if (response.ResultCase == AssetSearchResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(GetAssetByIdAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == AssetSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Assets.SearchAsync(new AssetSearchRequest { Id = assetId }));

                if (result.ResultCase != AssetSearchResponse.ResultOneofCase.Error)
                    return result.Body.Items.FirstOrDefault();

                _log.WriteError(nameof(GetAssetByIdAsync), new { assetId });
                return null;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(GetAssetByIdAsync), new { assetId }, ex);
                return null;
            }
        }

        public async Task<WhitelistItemCreateResponse> CreateWhitelistItemAsync(WhitelistItemCreateRequest request)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(CreateWhitelistItemAsync), new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<WhitelistItemCreateResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(CreateWhitelistItemAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.WriteInfo(nameof(CreateWhitelistItemAsync), info: "Creating whitelist item in sirius",
                    context: new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.WhitelistItems.CreateAsync(request));

                if (result.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                {
                    _log.WriteError(nameof(CreateWhitelistItemAsync), new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });
                    return null;
                }

                _log.WriteInfo(nameof(CreateWhitelistItemAsync), info: "Whitelist item created in siruis",
                    context: new { whitelistItemId = result.WhitelistItem.Id, requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });

                return result;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CreateWhitelistItemAsync), new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId }, ex);
                return null;
            }
        }

        public async Task<List<WhitelistItemResponse>> GetWhitelistItemsAsync(long accountId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(GetWhitelistItemsAsync), null, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<WhitelistItemsSearchResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(GetWhitelistItemsAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () =>
                    await _siriusApiClient.WhitelistItems.SearchAsync(new WhitelistItemSearchRequest
                    {
                        BrokerAccountId = _brokerAccountId,
                        AccountId = accountId,
                        IsRemoved = false,
                        TransactionType = { WhitelistTransactionType.Withdrawal }
                    }));

                if (result.BodyCase != WhitelistItemsSearchResponse.BodyOneofCase.Error)
                    return result.WhitelistItems.Items.ToList();

                _log.WriteError(nameof(GetWhitelistItemsAsync), null);
                return null;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(GetWhitelistItemsAsync), null, ex);
                return null;
            }
        }

        public async Task<WhitelistItemDeleteResponse> DeleteWhitelistItemsAsync(long id)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(DeleteWhitelistItemsAsync), new { requestId = id }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<WhitelistItemDeleteResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemDeleteResponse.BodyOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(DeleteWhitelistItemsAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.BodyCase == WhitelistItemDeleteResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.WhitelistItems.DeleteAsync(new WhitelistItemDeleteRequest
                    {
                        Id = id,
                        RequestId = id.ToString()
                    }
                ));

                if (result.BodyCase != WhitelistItemDeleteResponse.BodyOneofCase.Error)
                    return result;

                _log.WriteError(nameof(DeleteWhitelistItemsAsync), new { requestId = id });
                return null;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(DeleteWhitelistItemsAsync), new { requestId = id }, ex);
                return null;
            }
        }

        private async Task<AccountDetailsSearchResponse> SearchAccountDetailsAsync(string clientId, long accountId, long assetId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(SearchAccountDetailsAsync), new { clientId, accountId, assetId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<AccountDetailsSearchResponse>(response =>
                    {
                        if (response.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(SearchAccountDetailsAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.SearchDetailsAsync(
                    new AccountDetailsSearchRequest
                    {
                        BrokerAccountId = _brokerAccountId, AccountId = accountId, ReferenceId = clientId, AssetId = assetId
                    }
                ));

                if (result.ResultCase != AccountDetailsSearchResponse.ResultOneofCase.Error)
                    return result;

                _log.WriteError(nameof(SearchAccountDetailsAsync), new { clientId, accountId, assetId });
                return null;

            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(SearchAccountDetailsAsync), new { clientId, accountId, assetId }, ex);
                return null;
            }
        }

        private async Task<CreateUserResponse> CreateUserAsync(string clientId, string userRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(CreateUserAsync), new { clientId, requestId = userRequestId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<CreateUserResponse>(response =>
                    {
                        if (response.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(CreateUserAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.BodyCase == CreateUserResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.WriteInfo(nameof(CreateUserAsync), new { clientId, requestId = userRequestId }, "Creating user in sirius");

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Users.CreateAsync(
                    new CreateUserRequest { RequestId = userRequestId, NativeId = clientId }
                ));

                if (result.BodyCase != CreateUserResponse.BodyOneofCase.Error)
                    return result;

                _log.WriteError(nameof(CreateUserAsync), new { clientId, requestId = userRequestId });
                return null;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CreateUserAsync), new { clientId, requestId = userRequestId }, ex);
                return null;
            }
        }

        private async Task<AccountCreateResponse> CreateAccountAsync(string clientId, long userId, string accountRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.WriteWarning(nameof(CreateAccountAsync), new { clientId, userId, requestId = accountRequestId }, $"Retry on Exception: {ex.Message}.", ex);
                    return true;
                })
                .OrResult<AccountCreateResponse>(response =>
                    {
                        if (response.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                        {
                            _log.WriteWarning(nameof(CreateAccountAsync), response.ToJson(), "Response from sirius.");
                        }

                        return response.ResultCase == AccountCreateResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.WriteInfo(nameof(CreateAccountAsync), info: "Creating account in sirius",
                    context: new { clientId, requestId = accountRequestId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                    {
                        RequestId = accountRequestId, BrokerAccountId = _brokerAccountId, UserId = userId, ReferenceId = clientId
                    }
                ));

                if (result.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    _log.WriteError(nameof(CreateAccountAsync), new { clientId, userId, requestId = accountRequestId });
                    return null;
                }

                _log.WriteInfo(nameof(CreateWalletsAsync), info: "Account created in siruis",
                    context: new { account = result.Body.Account, clientId, requestId = accountRequestId });

                return result;
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CreateAccountAsync), new { clientId, userId, requestId = accountRequestId }, ex);
                return null;
            }
        }

        private async Task WaitForActiveWalletsAsync(string clientId, long? accountId)
        {
            _log.WriteInfo(nameof(WaitForActiveWalletsAsync),
                    info: $"Waiting for all wallets to be active ({_retryCount} retries with {_retryTimeout.TotalSeconds} sec. delay)",
                    context: new { clientId });

            var waitAccountCreationPolicy = Policy
                .HandleResult<AccountSearchResponse>(res =>
                {
                    if (res != null && res.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                    {
                        _log.WriteWarning(nameof(WaitForActiveWalletsAsync), info: "Error getting account", context: new { res.Error, clientId });
                        return true;
                    }

                    var hasAllActiveWallets = res != null && res.Body.Items.Count > 0 &&
                                              res.Body.Items.All(x => x.State == AccountStateModel.Active);
                    _log.WriteInfo(nameof(WaitForActiveWalletsAsync),
                        info: !hasAllActiveWallets ? "Wallets not ready yet..." : "All wallets are active!", context: new { clientId });
                    return !hasAllActiveWallets;
                })
                .WaitAndRetryAsync(_retryCount, retryAttempt => _retryTimeout);

            await waitAccountCreationPolicy.ExecuteAsync(async () =>
            {
                var request = new AccountSearchRequest
                {
                    BrokerAccountId = _brokerAccountId,
                    UserNativeId = clientId,
                    ReferenceId = clientId,
                    Pagination = new PaginationInt64 { Limit = 100 }
                };

                if (accountId.HasValue)
                {
                    request.Id = accountId.Value;
                }

                return await _siriusApiClient.Accounts.SearchAsync(request);
            });

            _log.WriteInfo(nameof(WaitForActiveWalletsAsync), info: "All wallets are active!", context: new { clientId });
        }
    }
}
