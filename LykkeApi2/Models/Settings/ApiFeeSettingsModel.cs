using System.Collections.Generic;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using Newtonsoft.Json;

namespace LykkeApi2.Models.Settings
{
    public class ApiFeeSettingsModel
    {
        public double BankCardsFeeSizePercentage { get; set; }

        [JsonProperty("CashOut")]
        public List<CashoutFee> CashOut { get; set; }
    }
}