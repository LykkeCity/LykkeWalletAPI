using Common;
using Core.Accounts;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AzureRepositories.Account
{
    public class WalletEntity : TableEntity
    {
        public class TheWallet : IWallet
        {
            [JsonProperty("balance")]
            public double Balance { get; set; }

            [JsonProperty("asset")]
            public string AssetId { get; set; }

            [JsonProperty("reserved")]
            public double Reserved { get; set; }


            public static TheWallet Create(string assetId, double balance)
            {
                return new TheWallet
                {
                    AssetId = assetId,
                    Balance = balance
                };
            }
        }

        public static string GeneratePartitionKey()
        {
            return "ClientBalance";
        }

        public static string GenerateRowKey(string traderId)
        {
            return traderId;
        }

        public string ClientId => RowKey;

        public string Balances { get; set; }

        internal void UpdateBalance(string assetId, double balanceDelta)
        {
            var data = Get();
            var element = data.FirstOrDefault(itm => itm.AssetId == assetId);

            if (element != null)
            {
                element.Balance += balanceDelta;
                Balances = data.ToJson();
                return;
            }

            var list = new List<TheWallet>();
            list.AddRange(data);
            list.Add(TheWallet.Create(assetId, balanceDelta));
            Balances = list.ToJson();

        }

        internal static readonly TheWallet[] EmptyList = new TheWallet[0];

        internal TheWallet[] Get()
        {
            if (string.IsNullOrEmpty(Balances))
                return EmptyList;

            return Balances.DeserializeJson(() => EmptyList);
        }
        public static WalletEntity Create(string clientId)
        {
            return new WalletEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(clientId),

            };
        }
    }

}
