using Lykke.Service.Balances.Client;
using Lykke.Service.Balances.AutorestClient.Models;
using LykkeApi2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using LykkeApi2.Infrastructure;
using Xunit;

namespace Lykke.WalletApiv2.Tests.ClientBalances
{
    public class ClientBalancesTest
    {
        private WalletsController _controller;

        [Fact]
        public async Task GetClientBalancesByClientId_ReturnsOk()
        {
            var context = new Mock<IRequestContext>();
            var clientAccountService = new Mock<IClientAccountService>();
            var balancesClient = new Mock<IBalancesClient>();
            balancesClient.Setup(x => x.GetClientBalances(It.IsAny<string>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClient);

            _controller = new WalletsController(context.Object, clientAccountService.Object, balancesClient.Object);

            var result = await _controller.GetTradingWalletBalances();

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetClientBalancesByClientIdAndAssetId_ReturnsOk()
        {
            var context = new Mock<IRequestContext>();
            var clientAccountService = new Mock<IClientAccountService>();
            var balancesClient = new Mock<IBalancesClient>();
            balancesClient.Setup(x => x.GetClientBalanceByAssetId(It.IsAny<ClientBalanceByAssetIdModel>()))
                .Returns(CreateMockedResponseForClientBalances.GetAllBalancesForClientByAssetId);

            _controller = new WalletsController(context.Object, clientAccountService.Object, balancesClient.Object);

            var result = await _controller.GetTradindWalletBalanceByAssetId("USD");

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
