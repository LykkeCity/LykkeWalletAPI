using Common.Log;
using Lykke.Service.Balances.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using LykkeApi2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using LykkeApi2.Infrastructure;
using Xunit;

namespace Lykke.WalletApiv2.Tests.ClientBalances
{
    public class ClientBalancesTest
    {
        private ClientController _controller;

        [Fact]
        public async Task GetClientBalancesByClientId_ReturnsOk()
        {
            var logs = new Mock<ILog>();
            var context = new Mock<IRequestContext>();
            var walletsClient = new Mock<IBalancesClient>();
            walletsClient.Setup(x => x.GetClientBalances(It.IsAny<string>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClient);

            _controller = new ClientController(logs.Object, walletsClient.Object, null, null, null, context.Object);

            var result = await _controller.Get();

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClientBalancesByClientIdAndAssetId_ReturnsOk()
        {
            var logs = new Mock<ILog>();
            var context = new Mock<IRequestContext>();
            var walletsClient = new Mock<IBalancesClient>();
            walletsClient.Setup(x => x.GetClientBalanceByAssetId(It.IsAny<ClientBalanceByAssetIdModel>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClientByAssetId);

            _controller = new ClientController(logs.Object, walletsClient.Object, null, null, null, context.Object);

            var result = await _controller.GetClientBalanceByAssetId("USD");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
