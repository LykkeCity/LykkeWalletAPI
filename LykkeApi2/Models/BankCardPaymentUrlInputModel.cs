using System;
using AzureRepositories.PaymentSystem;
using Common;
using Core.PaymentSystem;
using Lykke.Service.PersonalData.Contract.Models;
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

        [JsonIgnore]
        public DepositOption DepositOptionEnum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DepositOption))
                    return Models.DepositOption.BankCard;

                return Enum.TryParse(DepositOption, out DepositOption tmpOption)
                    ? tmpOption
                    : Models.DepositOption.BankCard;
            }
        }

        public static BankCardPaymentUrlRequestModel Create(IPaymentTransaction pt, IPersonalData personalData)
        {
            if (pt.PaymentSystem != CashInPaymentSystem.CreditVoucher
                && pt.PaymentSystem != CashInPaymentSystem.Fxpaygate)
                throw new Exception("Credit voucher payment system is expect for transactionID:" + pt.Id);

            var info = pt.GetInfo<OtherPaymentInfo>();

            return new BankCardPaymentUrlRequestModel
            {
                Address = info.Address,
                Amount = pt.Amount,
                AssetId = pt.AssetId,
                City = info.City,
                Country = info.Country,
                Phone = personalData.ContactPhone,
                Email = personalData.Email,
                FirstName = info.FirstName,
                LastName = info.LastName,
                Zip = info.Zip
            };
        }

        public static BankCardPaymentUrlRequestModel Create(IPersonalData personalData)
        {
            return new BankCardPaymentUrlRequestModel
            {
                Address = personalData.Address,
                City = personalData.City,
                Phone = personalData.ContactPhone,
                Country = personalData.CountryFromPOA ?? personalData.Country,
                Email = personalData.Email,
                FirstName = personalData.FirstName,
                LastName = personalData.LastName,
                Zip = personalData.Zip
            };
        }

        public string GetCountryIso3Code()
        {
            if (string.IsNullOrWhiteSpace(Country))
                return null;

            if (CountryManager.HasIso3(Country))
                return Country;

            if (CountryManager.HasIso2(Country))
                return CountryManager.Iso2ToIso3(Country);

            throw new Exception($"Country code {Country} not found in CountryManager");
        }

        public string GetCountryIso2Code()
        {
            if (string.IsNullOrWhiteSpace(Country))
                return null;

            if (CountryManager.HasIso2(Country))
                return Country;

            if (CountryManager.HasIso3(Country))
                return CountryManager.Iso3ToIso2(Country);

            throw new Exception($"Country code {Country} not found in CountryManager");
        }
    }
}