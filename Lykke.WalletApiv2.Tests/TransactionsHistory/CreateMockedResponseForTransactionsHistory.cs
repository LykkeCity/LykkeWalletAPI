using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.WalletApiv2.Tests.TransactionsHistory
{
    public static class CreateMockedResponseForTransactionsHistory
    {
        public static Task<LimitOrder> GetLimitOrder()
        {
            var limitOrder = new LimitOrder()
            {
                CreatedAt = DateTime.Now,
                Price = 280.50,
                AssetPairId = "ETHUSD",
                Volume = -0.5,
                Status = "Matched",
                Straight = true,
                Id = "29a16081-2f1c-44d6-8dd3-72fa871f4bc7",
                ClientId = null,
                RemainingVolume = 0,
                MatchingId = "36f75086-fd4c-4af8-928a-562c0ded7d81"
            };

            return Task.FromResult(limitOrder);
        }

        public static Task<IEnumerable<ClientTrade>> GetOrders()
        {
            List<ClientTrade> trades = new List<ClientTrade>();
            trades.Add(new ClientTrade()
            {

            });

            return Task.FromResult(trades.AsEnumerable());
        }

        public static Task<IEnumerable<AssetPair>> GetAssetPairs()
        {
            List<AssetPair> trades = new List<AssetPair>();
            trades.Add(new AssetPair()
            {
                Id = "ETHUSD",
                Name = "ETH/USD",
                BaseAssetId = "ETH",
                QuotingAssetId = "USD",
                Accuracy = 5,
                InvertedAccuracy = 5,
                Source = "ETHBTC",
                Source2 = "BTCUSD",
                IsDisabled = false
            });

            return Task.FromResult(trades.AsEnumerable());
        }

        public static Task<IEnumerable<ClientTrade>> GetClientTrades()
        {
            List<ClientTrade> trades = new List<ClientTrade>();
            trades.Add(new ClientTrade()
            {
                LimitOrderId = "29a16081-2f1c-44d6-8dd3-72fa871f4bc7",
                IsLimitOrderResult = true,
                AssetId = "USD",
                IsHidden = false,
                ClientId = null
            });
            return Task.FromResult(trades.AsEnumerable());
        }

        public static Task<IEnumerable<Asset>> GetAssets()
        {
            List<Asset> assets = new List<Asset>();
            assets.Add(new Asset()
            {
                Accuracy = 2,
                Id = "USD"
            });

            return Task.FromResult(assets.AsEnumerable());
        }

    }
}
