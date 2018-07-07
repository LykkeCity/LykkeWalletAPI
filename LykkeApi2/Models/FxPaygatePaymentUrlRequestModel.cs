using System;
using JetBrains.Annotations;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Newtonsoft.Json;

namespace LykkeApi2.Models
{
    public class FxPaygatePaymentUrlRequestModel
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
        [CanBeNull]
        public string OkUrl { get; set; }
        [CanBeNull]
        public string FailUrl { get; set; }
        [CanBeNull]
        public string CancelUrl { get; set; }
    }
}