using System;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.PaymentSystem;
using Core.Constants;
using Core.PaymentSystem;
using Core.Services;
using Core.Settings;
using Core.Wallet;
using Lykke.Payments.Client;
using Lykke.Payments.Contracts;
using Lykke.Service.ClientAccount.Client;

namespace LkeServices.PaymentSystem
{
    public class PaymentSystemService : IPaymentSystemService
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IPaymentTransactionEventsLogRepository _paymentTransactionEventsLog;
        private readonly FxpaygateSettings _fxpaygateSettings;
        private readonly CreditVouchersSettings _creditVouchersSettings;

        public class PaymentSystemSelectionResult
        {
            public CashInPaymentSystem PaymentSystem { get; set; }
            public string ServiceUrl { get; set; }
        }

        public PaymentSystemService(PaymentSystemsSettings paymentSystemsSettings,
            IClientAccountClient clientAccountClient, IPaymentTransactionsRepository paymentTransactionsRepository,
            IPaymentTransactionEventsLogRepository paymentTransactionEventsLog)
        {
            _clientAccountClient = clientAccountClient;
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _paymentTransactionEventsLog = paymentTransactionEventsLog;
            _fxpaygateSettings = paymentSystemsSettings.Fxpaygate;
            _creditVouchersSettings = paymentSystemsSettings.CreditVouchers;
        }

        public async Task InsertPaymentTransactionAsync(IPaymentTransaction src)
        {
            await _paymentTransactionsRepository.InsertAsync(src);
        }

        public async Task<IPaymentTransaction> GetLastPaymentTransactionByDate(string clientId)
        {
            return await _paymentTransactionsRepository.GetLastByDate(clientId);
        }

        public async Task InsertPaymentTransactionEventLogAsync(IPaymentTransactionEventLog newEvent)
        {
            await _paymentTransactionEventsLog.InsertAsync(newEvent);
        }

        public IPaymentTransactionEventLog CreatePaymentTransactionEventLog(string transactionId, string techData, string message, string who)
        {
            return new PaymentTransactionEventLog
            {
                PaymentTransactionId = transactionId,
                DateTime = DateTime.UtcNow,
                Message = message,
                TechData = techData,
                Who = who
            };
        }

        public IPaymentTransaction CreatePaymentTransaction(string id, CashInPaymentSystem paymentSystem, string clientId, double amount,
            double feeAmount, string assetId, string walletId, string assetToDeposit = null, string info = "",
            PaymentStatus status = PaymentStatus.Created)
        {
            return new PaymentTransaction
            {
                Id = id,
                PaymentSystem = paymentSystem,
                ClientId = clientId,
                Amount = amount,
                AssetId = assetId,
                WalletId = walletId,
                Created = DateTime.UtcNow,
                Status = status,
                Info = info,
                DepositedAssetId = assetToDeposit ?? assetId,
                FeeAmount = feeAmount
            };
        }
      
        public async Task<PaymentUrlData> GetUrlDataAsync(
            string clientPaymentSystem,
            string orderId,
            string clientId,
            double amount,
            string assetId,
            string walletId,
            string isoCountryCode,
            string otherInfoJson)
        {
            var owner = OwnerType.Spot;

            if (!string.IsNullOrEmpty(walletId))
            {
                var wallet = await _clientAccountClient.GetWalletAsync(walletId);

                if (wallet == null)
                    throw new Exception($"Wallet with ID {walletId} was not found");

                if (!Enum.TryParse(wallet.Owner, out owner))
                {
                    throw new Exception($"Owner {wallet.Owner} is not supported");
                }
            }

            var selection = Select(assetId, isoCountryCode, clientPaymentSystem, owner);

            if (!IsPaymentSystemSupported(selection.PaymentSystem, assetId))
                return new PaymentUrlData
                {
                    PaymentSystem = selection.PaymentSystem,
                    ErrorMessage = $"Asset {assetId} is not supported by {selection.PaymentSystem} payment system.",
                };

            GetUrlDataResult urlData;
            using (var paymentService = new PaymentGatewayServiceClient(selection.ServiceUrl))
            {
                urlData = await paymentService.GetUrlData(
                    orderId,
                    clientId,
                    amount,
                    assetId,
                    otherInfoJson);
            }

            var result = new PaymentUrlData
            {
                PaymentUrl = urlData.PaymentUrl,
                OkUrl = urlData.OkUrl,
                FailUrl = urlData.FailUrl,
                ReloadRegexp = urlData.ReloadRegexp,
                UrlsRegexp = urlData.UrlsRegexp,
                ErrorMessage = urlData.ErrorMessage,
                PaymentSystem = selection.PaymentSystem,
            };
            return result;
        }

        public async Task<string> GetSourceClientIdAsync(CashInPaymentSystem paymentSystem, OwnerType owner)
        {
            string serviceUrl = paymentSystem == CashInPaymentSystem.CreditVoucher
                ? SelectCreditVouchersService()
                : SelectFxPaygateService(owner);
            using (var paymentService = new PaymentGatewayServiceClient(serviceUrl))
            {
                return await paymentService.GetSourceClientId();
            }
        }

        private bool IsPaymentSystemSupported(CashInPaymentSystem paymentSystem, string assetId)
        {
            switch (paymentSystem)
            {
                case CashInPaymentSystem.CreditVoucher:
                    return _creditVouchersSettings.SupportedCurrencies?.Contains(assetId) ?? assetId == LykkeConstants.UsdAssetId;
                case CashInPaymentSystem.Fxpaygate:
                    return _fxpaygateSettings.SupportedCurrencies?.Contains(assetId) ?? assetId == LykkeConstants.UsdAssetId;
                default:
                    return false;
            }
        }

        private PaymentSystemSelectionResult Select(string assetId, string isoCountryCode, string clientPaymentSystem, OwnerType owner)
        {
            switch (owner)
            {
                case OwnerType.Mt_LYKKEVU:
                    return new PaymentSystemSelectionResult
                    {
                        PaymentSystem = CashInPaymentSystem.Fxpaygate,
                        ServiceUrl = SelectFxPaygateService(OwnerType.Mt_LYKKEVU)
                    };
                case OwnerType.Mt_LYKKECY:
                    return new PaymentSystemSelectionResult
                    {
                        PaymentSystem = CashInPaymentSystem.Fxpaygate,
                        ServiceUrl = SelectFxPaygateService(OwnerType.Mt_LYKKECY)
                    };
            }

            CashInPaymentSystem paymentSystem = CashInPaymentSystem.CreditVoucher;
            string serviceUrl = SelectCreditVouchersService();

            CardPaymentSystem byClient = CardPaymentSystem.Unknown;
            bool hasClientFixedSystem = false;
            if (!string.IsNullOrWhiteSpace(clientPaymentSystem))
                hasClientFixedSystem = Enum.TryParse(clientPaymentSystem, out byClient);

            if (hasClientFixedSystem && byClient == CardPaymentSystem.Fxpaygate
                || (!hasClientFixedSystem || byClient != CardPaymentSystem.CreditVoucher)
                && _fxpaygateSettings.Currencies.Contains(assetId)
                && _fxpaygateSettings.Countries.Contains(isoCountryCode))
            {
                paymentSystem = CashInPaymentSystem.Fxpaygate;
                serviceUrl = SelectFxPaygateService(OwnerType.Spot);
            }

            return new PaymentSystemSelectionResult
            {
                PaymentSystem = paymentSystem,
                ServiceUrl = serviceUrl,
            };
        }

        private string SelectFxPaygateService(OwnerType owner)
        {
            if (!_fxpaygateSettings.ServiceUrls.TryGetValue(owner, out var result))
            {
                throw new NotSupportedException($"Owner {owner} is not supported by FxPaygate");
            }

            return result;
        }

        private string SelectCreditVouchersService()
        {
            return _creditVouchersSettings.ServiceUrls[0];
        }
    }
}