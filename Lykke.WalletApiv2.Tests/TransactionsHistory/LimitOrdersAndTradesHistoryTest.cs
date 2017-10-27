using Common;
using Common.Log;
using Core.Mappers;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Lykke.Service.OperationsRepository.Client.Abstractions.Exchange;
using LykkeApi2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Balances.Client;
using LykkeApi2.Infrastructure;
using Xunit;
using Lykke.Service.OperationsRepository.Client.Abstractions.OperationsDetails;

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
            var requestContext = new Mock<IRequestContext>();

            var operationDetailsInformationClient = new Mock<IOperationDetailsInformationClient>();

            var walletsClient = new Mock<IBalancesClient>();
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
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssets()).ToDictionary(itm => itm.Id)), requestContext.Object, operationDetailsInformationClient.Object);

            var result = await _controller.LimitOrder("29a16081-2f1c-44d6-8dd3-72fa871f4bc7");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetLimitTradesHistory_ReturnsOk()
        {
            var logs = new Mock<ILog>();

            var tradeOperationsRepositoryClient = new Mock<ITradeOperationsRepositoryClient>();
            var transferOperationsRepositoryClient = new Mock<ITransferOperationsRepositoryClient>();
            var cashOperationsRepositoryClient = new Mock<ICashOperationsRepositoryClient>();
            var cashOutAttemptOperationsRepositoryClient = new Mock<ICashOutAttemptOperationsRepositoryClient>();
            var limitTradeEventsRepositoryClient = new Mock<ILimitTradeEventsRepositoryClient>();
            var limitOrdersRepositoryClient = new Mock<ILimitOrdersRepositoryClient>();
            var marketOrdersRepositoryClient = new Mock<IMarketOrdersRepositoryClient>();
            var requestContext = new Mock<IRequestContext>();
            var walletsClient = new Mock<IBalancesClient>();
            var operationsHistoryClient = new Mock<IOperationsHistoryClient>();
            var historyOperationMapper = new Mock<IHistoryOperationMapper<object, HistoryOperationSourceData>>();

            var operationDetailsInformationClient = new Mock<IOperationDetailsInformationClient>();

            tradeOperationsRepositoryClient.Setup(x => x.GetByOrderAsync(It.IsAny<string>()))
                .Returns(CreateMockedResponseForTransactionsHistory.GetClientTrades());

            _controller = new TransactionHistoryController(logs.Object, tradeOperationsRepositoryClient.Object,
               transferOperationsRepositoryClient.Object, cashOperationsRepositoryClient.Object, cashOutAttemptOperationsRepositoryClient.Object,
               limitTradeEventsRepositoryClient.Object, limitOrdersRepositoryClient.Object, marketOrdersRepositoryClient.Object,
               walletsClient.Object, operationsHistoryClient.Object, historyOperationMapper.Object, new CachedDataDictionary<string, IAssetPair>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssetPairs()).ToDictionary(itm => itm.Id)),
              new CachedDataDictionary<string, IAsset>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssets()).ToDictionary(itm => itm.Id)), requestContext.Object, operationDetailsInformationClient.Object);

            var result = await _controller.LimitTrades("29a16081-2f1c-44d6-8dd3-72fa871f4bc7");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetOperationsDetailsHistory_ReturnsOk()
        {
            var logs = new Mock<ILog>();

            var tradeOperationsRepositoryClient = new Mock<ITradeOperationsRepositoryClient>();
            var transferOperationsRepositoryClient = new Mock<ITransferOperationsRepositoryClient>();
            var cashOperationsRepositoryClient = new Mock<ICashOperationsRepositoryClient>();
            var cashOutAttemptOperationsRepositoryClient = new Mock<ICashOutAttemptOperationsRepositoryClient>();
            var limitTradeEventsRepositoryClient = new Mock<ILimitTradeEventsRepositoryClient>();
            var limitOrdersRepositoryClient = new Mock<ILimitOrdersRepositoryClient>();
            var marketOrdersRepositoryClient = new Mock<IMarketOrdersRepositoryClient>();
            var requestContext = new Mock<IRequestContext>();
            var walletsClient = new Mock<IBalancesClient>();
            var operationsHistoryClient = new Mock<IOperationsHistoryClient>();
            var historyOperationMapper = new Mock<IHistoryOperationMapper<object, HistoryOperationSourceData>>();

            var operationDetailsInformationClient = new Mock<IOperationDetailsInformationClient>();

            operationDetailsInformationClient.Setup(x => x.GetAsync(It.IsAny<string>()))
                .Returns(CreateMockedResponseForOperationsDetailsHistory.GetOperationsDetails());

            _controller = new TransactionHistoryController(logs.Object, tradeOperationsRepositoryClient.Object,
               transferOperationsRepositoryClient.Object, cashOperationsRepositoryClient.Object, cashOutAttemptOperationsRepositoryClient.Object,
               limitTradeEventsRepositoryClient.Object, limitOrdersRepositoryClient.Object, marketOrdersRepositoryClient.Object,
               walletsClient.Object, operationsHistoryClient.Object, historyOperationMapper.Object, new CachedDataDictionary<string, IAssetPair>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssetPairs()).ToDictionary(itm => itm.Id)),
              new CachedDataDictionary<string, IAsset>(
                  async () => (await CreateMockedResponseForTransactionsHistory.GetAssets()).ToDictionary(itm => itm.Id)), requestContext.Object, operationDetailsInformationClient.Object);

            var result = await _controller.OperationsDetailHistory("4e276be2-5fb8-438d-9d73-15687a84d5e9", "b64e64bb-eff6-43aa-aee8-d37e8dda7bed");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
