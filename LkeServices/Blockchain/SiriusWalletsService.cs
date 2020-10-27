using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Blockchain;
using Polly;
using Polly.Retry;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Common;

namespace LkeServices.Blockchain
{
    public class SiriusWalletsService : ISiriusWalletsService
    {
        private readonly long _brokerAccountId;
        private readonly int _retryCount;
        private readonly TimeSpan _retryTimeout;
        private readonly IApiClient _siriusApiClient;
        private readonly ILog _log;

        private readonly RetryPolicy<AccountSearchResponse> _waitAccountCreationPolicy;

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

            _waitAccountCreationPolicy = Policy
                .HandleResult<AccountSearchResponse>(res =>
                {
                    if (res != null && res.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                    {
                        _log.WriteInfo(nameof(CreateWalletsAsync), info: "Error getting account", context: $"error: {res.Error.ToJson()}");
                        return true;
                    }

                    var hasAllActiveWallets = res != null && res.Body.Items.Count > 0 && res.Body.Items.All(x => x.State == AccountStateModel.Active);
                    _log.WriteInfo(nameof(CreateWalletsAsync),info: !hasAllActiveWallets ? "Wallets not ready yet..." : "All wallets are active!", context: null);
                    return !hasAllActiveWallets;
                })
                .WaitAndRetryAsync(_retryCount, retryAttempt => _retryTimeout);
        }

        public async Task CreateWalletsAsync(string clientId, bool waitForCreation)
        {
            var accountResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                ReferenceId = clientId,
                Pagination = new PaginationInt64{Limit = 100}
            });

            if (accountResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error getting wallets from sirius", context: $"Error: {accountResponse.Error.ToJson()}");
            }

            if (accountResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Body &&
                accountResponse.Body.Items.Count == 0)
            {
                _log.WriteInfo(nameof(CreateWalletsAsync), info: "Creating wallets in sirius", context: clientId);

                var createResponse = await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                {
                    RequestId = $"{_brokerAccountId}{clientId}",
                    BrokerAccountId = _brokerAccountId,
                    ReferenceId = clientId
                });

                if (createResponse.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    _log.WriteWarning(nameof(CreateWalletsAsync), info: "Error creating wallets in sirius", context: $"Error: {createResponse.Error.ToJson()}");
                    return;
                }

                _log.WriteInfo(nameof(CreateWalletsAsync), info: "Wallets created in siruis", context: $"Result: {createResponse.Body.Account.ToJson()}");

                if (waitForCreation)
                {
                    _log.WriteInfo(nameof(CreateWalletsAsync), info: $"Waiting for all wallets to be active ({_retryCount} retries with {_retryTimeout.TotalSeconds} sec. delay)", context: $"clientId: {clientId}");

                    await _waitAccountCreationPolicy.ExecuteAsync(async () =>
                        await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
                        {
                            BrokerAccountId = _brokerAccountId,
                            Id = createResponse.Body.Account.Id,
                            ReferenceId = clientId,
                            Pagination = new PaginationInt64{Limit = 100}
                        }));

                    _log.WriteInfo(nameof(CreateWalletsAsync), info: "All wallets are active!", context: $"clientId: {clientId}");
                }
            }
        }

        public async Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId)
        {
            var searchResponse = await _siriusApiClient.Accounts.SearchDetailsAsync(new AccountDetailsSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                ReferenceId = clientId,
                AssetId = assetId
            });

            if (searchResponse.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error)
            {
                _log.WriteWarning(nameof(GetWalletAdderssAsync), info: "Error getting wallet from sirius", context: $"Error: {searchResponse.Error.ToJson()}, clientId: {clientId}, assetId: {assetId}");
            }

            return searchResponse.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Body
                ? searchResponse.Body.Items.FirstOrDefault()
                : null;
        }
    }
}
