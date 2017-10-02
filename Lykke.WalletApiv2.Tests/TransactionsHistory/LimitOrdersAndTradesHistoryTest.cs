using Common;
using Common.Log;
using Core.Mappers;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Lykke.Service.OperationsRepository.Client.Abstractions.Exchange;
using Lykke.Service.Wallets.Client;
using LykkeApi2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lykke.WalletApiv2.Tests.TransactionsHistory
{
    public class LimitOrdersAndTradesHistoryTest
    {
        private TransactionHistoryController _controller;

        [Fact]
        public async Task GetLimitOrdersHistory_ReturnsOk()
        {
            var logs = new Mock<ILog>();

            var tradeOperationsRepositoryClient = new Mock<ITradeOperationsRepositoryClient>();
            var transferOperationsRepositoryClient = new Mock<ITransferOperationsRepositoryClient>();
            var cashOperationsRepositoryClient = new Mock<ICashOperationsRepositoryClient>();
            var cashOutAttemptOperationsRepositoryClient = new Mock<ICashOutAttemptOperationsRepositoryClient>();
            var limitTradeEventsRepositoryClient = new Mock<ILimitTradeEventsRepositoryClient>();
            var limitOrdersRepositoryClient = new Mock<ILimitOrdersRepositoryClient>();
            var marketOrdersRepositoryClient = new Mock<IMarketOrdersRepositoryClient>();

            var walletsClient = new Mock<IWalletsClient>();
            var operationsHistoryClient = new Mock<IOperationsHistoryClient>();
            var historyOperationMapper = new Mock<IHistoryOperationMapper<object, HistoryOperationSourceData>>();

            limitOrdersRepositoryClient.Setup(x => x.GetOrderAsync(It.IsAny<string>()))
                .Returns(CreateMockedResponseForTransactionsHistory.GetLimitOrder());

            _controller = new TransactionHistoryController(logs.Object, tradeOperationsRepositoryClient.Object,
               transferOperationsRepositoryClient.Object, cashOperationsRepositoryClient.Object, cashOutAttemptOperationsRepositoryClient.Object,
               limitTradeEventsRepositoryClient.Object, limitOrdersRepositoryClient.Object, marketOrdersRepositoryClient.Object,
               walletsClient.Object, operationsHistoryClient.Object, historyOperationMapper.Object, new CachedDataDictionary<string, IAssetPair>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssetPairs()).ToDictionary(itm => itm.Id)),
              new CachedDataDictionary<string, IAsset>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssets()).ToDictionary(itm => itm.Id)));

            var result = await _controller.LimitOrder("29a16081-2f1c-44d6-8dd3-72fa871f4bc7");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
