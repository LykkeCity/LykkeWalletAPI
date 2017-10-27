using Lykke.Service.Assets.Client.Custom;
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

        public static Task<IEnumerable<IAssetPair>> GetAssetPairs()
        {
            List<IAssetPair> trades = new List<IAssetPair>();
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

        public static Task<IEnumerable<Service.Assets.Client.Custom.IAsset>> GetAssets()
        {
            List<Service.Assets.Client.Custom.IAsset> assets = new List<Service.Assets.Client.Custom.IAsset>();
            assets.Add(new Asset()
            {
                Accuracy = 2,
                Id = "USD"
            });

            return Task.FromResult(assets.AsEnumerable());
        }

    }

    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }
        public string Source { get; set; }
        public string Source2 { get; set; }
        public bool IsDisabled { get; set; }
    }

    public class Asset : Service.Assets.Client.Custom.IAsset
    {
        public string Id { get; set; }
        public bool NotLykkeAsset { get; set; }
        public bool IssueAllowed { get; set; }
        public double? LowVolumeAmount { get; set; }
        public string DisplayId { get; set; }
        public bool BankCardsDepositEnabled { get; set; }
        public bool SwiftDepositEnabled { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public bool BuyScreen { get; set; }
        public bool SellScreen { get; set; }
        public bool BlockchainWithdrawal { get; set; }
        public bool SwiftWithdrawal { get; set; }
        public bool ForwardWithdrawal { get; set; }
        public bool CrosschainWithdrawal { get; set; }
        public int ForwardFrozenDays { get; set; }
        public string ForwardBaseAsset { get; set; }
        public IList<string> PartnerIds { get; set; }
        public string DefinitionUrl { get; set; }
        public Blockchain Blockchain { get; set; }
        public string CategoryId { get; set; }
        public string BlockChainId { get; set; }
        public string BlockChainAssetId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string IdIssuer { get; set; }
        public bool IsBase { get; set; }
        public bool HideIfZero { get; set; }
        public string ForwardMemoUrl { get; set; }
        public int Accuracy { get; set; }
        public bool IsDisabled { get; set; }
        public bool HideWithdraw { get; set; }
        public bool HideDeposit { get; set; }
        public int DefaultOrder { get; set; }
        public bool KycNeeded { get; set; }
        public string AssetAddress { get; set; }
        public double DustLimit { get; set; }
        public int MultiplierPower { get; set; }
        public string IconUrl { get; set; }
    }
}
