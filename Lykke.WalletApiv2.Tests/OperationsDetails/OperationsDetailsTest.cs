using Common.Log;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.OperationsDetails;
using LykkeApi2.Controllers;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.OperationsDetailsModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Lykke.WalletApiv2.Tests.OperationsDetails
{
    public class OperationsDetailsTest
    {
        private OperationsDetailsController _controller;

        [Fact]
        public async Task CreateOperationDetails_ReturnsOk()
        {
            var context = new Mock<IRequestContext>();
            var operationDetailsInformationClient = new Mock<IOperationDetailsInformationClient>();
            var logs = new Mock<ILog>();


            operationDetailsInformationClient.Setup(x => x.CreateAsync(It.IsAny<OperationDetailsInformation>()))
             .Returns(CreateMockedResponseForOperationsDetails.CreateOperation());

            _controller = new OperationsDetailsController(logs.Object, context.Object,
                            operationDetailsInformationClient.Object);

            var result = await _controller.CreateOperationsDetail(new OperationsDetailsModel()
            {
                TransactionId = " de8fd8c3-5115-488d-be0a-564389ea5365",
                Comment = "Test new comment"
            });

            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RegisterOperationDetails_ReturnsOk()
        {
            var context = new Mock<IRequestContext>();
            var operationDetailsInformationClient = new Mock<IOperationDetailsInformationClient>();
            var logs = new Mock<ILog>();


            _controller = new OperationsDetailsController(logs.Object, context.Object,
                            operationDetailsInformationClient.Object);

            operationDetailsInformationClient.Setup(x => x.RegisterAsync(It.IsAny<OperationDetailsInformation>()))
                                                            .Returns(CreateMockedResponseForOperationsDetails.RegisterOerationDetail());

            var result = await _controller.RegisterOperationsDetail(new OperationsDetailsModel()
            {
                TransactionId = " de8fd8c3-5115-488d-be0a-564389ea5365",
                Comment = "Test new comment"
            });

            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }

}
