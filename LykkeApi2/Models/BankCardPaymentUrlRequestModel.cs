using System;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Newtonsoft.Json;

namespace LykkeApi2.Models
{
    public class BankCardPaymentUrlRequestModel
    {
        /// <summary>
        /// Amount in currency defined by CurrencyCode
        /// </summary>
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public string WalletId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string Address { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DepositOption { get; set; }
        public string OkUrl { get; set; }
        public string FailUrl { get; set; }

        [JsonIgnore]
        public DepositOption DepositOptionEnum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DepositOption))
                    return Lykke.Service.PaymentSystem.Client.AutorestClient.Models.DepositOption.BankCard;

                return Enum.TryParse(DepositOption, out DepositOption tmpOption)
                    ? tmpOption
                    : Lykke.Service.PaymentSystem.Client.AutorestClient.Models.DepositOption.BankCard;
            }
        }
    }
}