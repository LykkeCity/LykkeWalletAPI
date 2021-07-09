using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Blockchain;
using Google.Protobuf.WellKnownTypes;
using Polly;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Address;
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
        }

        public async Task CreateWalletsAsync(string clientId)
        {
            long? accountId = null;
            
            var accountResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                UserNativeId = clientId,
                Pagination = new PaginationInt64{Limit = 100},
                ReferenceId = clientId
            });;

            if (accountResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Body)
            {
                if (accountResponse.Body.Items.Count == 0)
                {
                    string accountRequestId = $"{_brokerAccountId}_{clientId}_account";
                    string userRequestId = $"{clientId}_user";

                    _log.WriteInfo(nameof(CreateWalletsAsync), info: "Creating user in sirius", context: new { clientId, requestId = userRequestId });

                    var user = await _siriusApiClient.Users.CreateAsync(new CreateUserRequest
                    {
                        RequestId = userRequestId,
                        NativeId = clientId
                    });

                    if (user.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                    {
                        _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating user in sirius", context: new { error = user.Error, clientId, requestId = userRequestId });
                        return;
                    }

                    _log.WriteInfo(nameof(CreateWalletsAsync), info: "Creating wallets in sirius", context: new { clientId, requestId = accountRequestId });

                    var createResponse = await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                    {
                        RequestId = accountRequestId,
                        BrokerAccountId = _brokerAccountId,
                        UserId = user.User.Id,
                        ReferenceId = clientId
                    });

                    if (createResponse.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                    {
                        _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating wallets in sirius", context: new { error = createResponse.Error, clientId, requestId = accountRequestId });
                        return;
                    }

                    _log.WriteInfo(nameof(CreateWalletsAsync), info: "Wallets created in siruis", context: new { account = createResponse.Body.Account, clientId, requestId = accountRequestId });

                    accountId = createResponse.Body.Account.Id;
                    
                    _log.WriteInfo(nameof(CreateWalletsAsync), info: $"Waiting for all wallets to be active ({_retryCount} retries with {_retryTimeout.TotalSeconds} sec. delay)", context: new { clientId });

                    var waitAccountCreationPolicy = Policy
                        .HandleResult<AccountSearchResponse>(res =>
                        {
                            if (res != null && res.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                            {
                                _log.WriteInfo(nameof(CreateWalletsAsync), info: "Error getting account", context: new { res.Error, clientId });
                                return true;
                            }

                            var hasAllActiveWallets = res != null && res.Body.Items.Count > 0 && res.Body.Items.All(x => x.State == AccountStateModel.Active);
                            _log.WriteInfo(nameof(CreateWalletsAsync),info: !hasAllActiveWallets ? "Wallets not ready yet..." : "All wallets are active!", context: new { clientId });
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
                                Pagination = new PaginationInt64 {Limit = 100}
                            };

                            if (accountId.HasValue)
                            {
                                request.Id = accountId.Value;
                            }

                            return await _siriusApiClient.Accounts.SearchAsync(request);
                        });

                    _log.WriteInfo(nameof(CreateWalletsAsync), info: "All wallets are active!", context: new { clientId });
                }

                var whitelistingRequestId = $"lykke:trading_wallet:{clientId}";
                
                var whitelistItemCreateResponse = await _siriusApiClient.WhitelistItems.CreateAsync(new WhitelistItemCreateRequest
                {
                    Name = "Trading Wallet Whitelist",
                    Scope = new WhitelistItemScopeModel
                    {
                        BrokerAccountId = _brokerAccountId,
                        AccountId = accountId,
                        UserNativeId = clientId
                    },
                    Details = new WhitelistItemDetailsModel
                    {
                        TransactionType = WhitelistTransactionTypeModel.Any,
                        TagType = new NullableWhitelistItemTagModel
                        {
                            Null = NullValue.NullValue
                        }
                    },
                    Lifespan = new WhitelistItemLifespanModel
                    {
                        StartsAt = Timestamp.FromDateTime(DateTime.UtcNow)
                    },
                    RequestId = whitelistingRequestId
                });

                if (whitelistItemCreateResponse.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                {
                    _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating Whitelist item", context: new { error = whitelistItemCreateResponse.Error, clientId });
                }
            }
            else
            {
                _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error getting wallets from sirius", context: new { error = accountResponse.Error, clientId });
            }

            
        }

        public async Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId)
        {
            var userResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                UserNativeId = clientId,
                ReferenceId = clientId
            });

            if (userResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                _log.WriteWarning(nameof(GetWalletAdderssAsync), info: "Error getting user from sirius", context: new { error = userResponse.Error, clientId });
                return null;
            }

            var accountId = userResponse.Body.Items.FirstOrDefault()?.Id;

            if (accountId == null)
                return null;

            var searchResponse = await _siriusApiClient.Accounts.SearchDetailsAsync(new AccountDetailsSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                AccountId = accountId,
                ReferenceId = clientId,
                AssetId = assetId
            });

            if (searchResponse.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error)
            {
                _log.WriteWarning(nameof(GetWalletAdderssAsync), info: "Error getting wallet from sirius", context: new { error = searchResponse.Error, accountId, clientId, assetId });
            }

            return searchResponse.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Body
                ? searchResponse.Body.Items.FirstOrDefault()
                : null;
        }

        public async Task<bool> IsAddressValidAsync(string blockchainId, string address)
        {
            var response = await _siriusApiClient.Addresses.IsValidAsync(new AddressIsValidRequest
            {
                Address = address,
                BlockchainId = blockchainId
            });

            return response.ResultCase == AddressIsValidResponse.ResultOneofCase.Body && response.Body.IsValid;
        }
    }
}
