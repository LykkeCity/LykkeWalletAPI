using Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Bitcoin
{
    public class WalletCredentialsEntity : TableEntity, IWalletCredentials
    {
        public static class ByClientId
        {
            public static string GeneratePartitionKey()
            {
                return "Wallet";
            }

            public static string GenerateRowKey(string clientId)
            {
                return clientId;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ClientId);
                return entity;
            }
        }

        public static class ByColoredMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletColoredMultisig";
            }

            public static string GenerateRowKey(string coloredMultisig)
            {
                return coloredMultisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ColoredMultiSig);
                return entity;
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletMultisig";
            }

            public static string GenerateRowKey(string multisig)
            {
                return multisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.MultiSig);
                return entity;
            }
        }

        public static class ByEthContract
        {
            public static string GeneratePartitionKey()
            {
                return "EthConversionWallet";
            }

            public static string GenerateRowKey(string ethWallet)
            {
                return ethWallet;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.EthConversionWalletAddress);
                return entity;
            }
        }

        public static class BySolarCoinWallet
        {
            public static string GeneratePartitionKey()
            {
                return "SolarCoinWallet";
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.SolarCoinWalletAddress);
                return entity;
            }
        }

        public static class ByChronoBankContract
        {
            public static string GeneratePartitionKey()
            {
                return "ChronoBankContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ChronoBankContract);
                return entity;
            }
        }

        public static class ByQuantaContract
        {
            public static string GeneratePartitionKey()
            {
                return "QuantaContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.QuantaContract);
                return entity;
            }
        }

        public static WalletCredentialsEntity Create(IWalletCredentials src)
        {
            return new WalletCredentialsEntity
            {
                ClientId = src.ClientId,
                PrivateKey = src.PrivateKey,
                Address = src.Address,
                MultiSig = src.MultiSig,
                ColoredMultiSig = src.ColoredMultiSig,
                PreventTxDetection = src.PreventTxDetection,
                EncodedPrivateKey = src.EncodedPrivateKey,
                PublicKey = src.PublicKey,
                BtcConvertionWalletPrivateKey = src.BtcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = src.BtcConvertionWalletAddress,
                EthConversionWalletAddress = src.EthConversionWalletAddress,
                EthAddress = src.EthAddress,
                EthPublicKey = src.EthPublicKey,
                SolarCoinWalletAddress = src.SolarCoinWalletAddress,
                ChronoBankContract = src.ChronoBankContract,
                QuantaContract = src.QuantaContract
            };
        }

        public static void Update(WalletCredentialsEntity src, IWalletCredentials changed)
        {
            src.ClientId = changed.ClientId;
            src.PrivateKey = changed.PrivateKey;
            src.Address = changed.Address;
            src.MultiSig = changed.MultiSig;
            src.ColoredMultiSig = changed.ColoredMultiSig;
            src.PreventTxDetection = changed.PreventTxDetection;
            src.EncodedPrivateKey = changed.EncodedPrivateKey;
            src.PublicKey = changed.PublicKey;
            src.BtcConvertionWalletPrivateKey = changed.BtcConvertionWalletPrivateKey;
            src.BtcConvertionWalletAddress = changed.BtcConvertionWalletAddress;
            src.EthConversionWalletAddress = changed.EthConversionWalletAddress;
            src.EthAddress = changed.EthAddress;
            src.EthPublicKey = changed.EthPublicKey;
            src.SolarCoinWalletAddress = changed.SolarCoinWalletAddress;
            src.ChronoBankContract = changed.ChronoBankContract;
            src.QuantaContract = changed.QuantaContract;
        }

        public string ClientId { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string MultiSig { get; set; }
        public string ColoredMultiSig { get; set; }
        public bool PreventTxDetection { get; set; }
        public string EncodedPrivateKey { get; set; }
        public string BtcConvertionWalletPrivateKey { get; set; }
        public string BtcConvertionWalletAddress { get; set; }
        public string EthConversionWalletAddress { get; set; }
        public string EthAddress { get; set; }
        public string EthPublicKey { get; set; }
        public string SolarCoinWalletAddress { get; set; }
        public string ChronoBankContract { get; set; }
        public string QuantaContract { get; set; }
    }
}