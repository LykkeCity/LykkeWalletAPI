using Common.Log;
using Lykke.Service.Wallets.Client;
using Lykke.Service.Wallets.Client.AutorestClient.Models;
using LykkeApi2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Lykke.WalletApiv2.Tests.ClientBalances
{
    public class ClientBalancesTest
    {
        private ClientBalancesController _controller;

        [Fact]
        public async Task GetClientBalancesByClientId_ReturnsOk()
        {
            var logs = new Mock<ILog>();
            var walletsClient = new Mock<IWalletsClient>();
            walletsClient.Setup(x => x.GetClientBalances(It.IsAny<string>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClient);

            _controller = new ClientBalancesController(logs.Object, walletsClient.Object);

            var result = await _controller.Get();

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClientBalancesByClientIdAndAssetId_ReturnsOk()
        {
            var logs = new Mock<ILog>();
            var walletsClient = new Mock<IWalletsClient>();
            walletsClient.Setup(x => x.GetClientBalanceByAssetId(It.IsAny<ClientBalanceByAssetIdModel>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClientByAssetId);

            _controller = new ClientBalancesController(logs.Object, walletsClient.Object);

            var result = await _controller.GetClientBalnceByAssetId("USD");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
