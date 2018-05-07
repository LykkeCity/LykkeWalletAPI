// Copyright 2017 Lykke Corp.
// See LICENSE file in the project root for full license information.

namespace LykkeApi2.Tests.Controllers
{
    using System.Threading.Tasks;
    using Common.Log;
    using Core.Identity;
    using Core.Settings;
    using FakeItEasy;
    using FluentAssertions;
    using Lykke.Service.ClientAccount.Client;
    using Lykke.Service.ClientAccount.Client.Models;
    using Lykke.Service.Kyc.Abstractions.Services;
    using Lykke.Service.PersonalData.Contract;
    using Lykke.Service.PersonalData.Contract.Models;
    using Lykke.Service.Registration;
    using Lykke.Service.Registration.Models;
    using Lykke.Service.Session.Client;
    using Lykke.Service.Session.Contracts;
    using LykkeApi2.Controllers;
    using LykkeApi2.Credentials;
    using LykkeApi2.Infrastructure;
    using LykkeApi2.Models.Auth;
    using LykkeApi2.Models.ClientAccountModels;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Xbehave;

    public class ClientTests
    {
        [Scenario]
        public void RegistrationHappyPath(ILykkeRegistrationClient lykkeRegistrationClient, ClientAccountLogic clientAccountLogic, ClientController clientController, Task<IActionResult> result)
        {
            "Given a lykke registraion client"
                .x(() =>
                {
                    lykkeRegistrationClient = A.Fake<ILykkeRegistrationClient>();
                    A.CallTo(() => lykkeRegistrationClient.RegisterAsync(A<RegistrationModel>.Ignored)).Returns(Task.FromResult(new RegistrationResponse()));
                });

            "And a client account logic"
                .x(() =>
                {
                    var clientAccountService = A.Fake<IClientAccountClient>();
                    A.CallTo(() => clientAccountService.GetClientByEmailAndPartnerIdAsync("example@example.com", null)).Returns(Task.FromResult((ClientAccountInformationModel)null));

                    clientAccountLogic = new ClientAccountLogic(clientAccountService);
                });

            "And a client controller"
                .x(() =>
                {
                    clientController = new ClientController(
                        A.Fake<ILog>(),
                        A.Fake<ILykkePrincipal>(),
                        A.Fake<IClientSessionsClient>(),
                        lykkeRegistrationClient,
                        clientAccountLogic,
                        A.Fake<IRequestContext>(),
                        A.Fake<IPersonalDataService>(),
                        A.Fake<IKycStatusService>(),
                        A.Fake<IClientAccountClient>(),
                        A.Fake<BaseSettings>());

                    clientController.ControllerContext = new ControllerContext();
                    clientController.ControllerContext.HttpContext = new DefaultHttpContext();
                    clientController.ControllerContext.HttpContext.Request.Host = new HostString("whatever");
                });

            "When I register a new client"
                .x(() =>
                {
                    var newRegistration = new AccountRegistrationModel()
                    {
                        Email = "example@example.com",
                        Password = "pasword1234"
                    };

                    result = clientController.Post(newRegistration);
                });

            "Then the result is OK"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                });
        }

        [Scenario]
        public void AuthHappyPath(ILykkeRegistrationClient lykkeRegistrationClient, ClientController clientController, Task<IActionResult> result)
        {
            "Given a lykke registraion client"
                .x(() =>
                {
                    lykkeRegistrationClient = A.Fake<ILykkeRegistrationClient>();
                    A.CallTo(() => lykkeRegistrationClient.AuthorizeAsync(A<AuthModel>.Ignored)).Returns(Task.FromResult(new AuthResponse()
                    {
                        Status = AuthenticationStatus.Ok
                    }));
                });

            "And a client controller"
                .x(() =>
                {
                    clientController = new ClientController(
                        A.Fake<ILog>(),
                        A.Fake<ILykkePrincipal>(),
                        A.Fake<IClientSessionsClient>(),
                        lykkeRegistrationClient,
                        A.Fake<ClientAccountLogic>(),
                        A.Fake<IRequestContext>(),
                        A.Fake<IPersonalDataService>(),
                        A.Fake<IKycStatusService>(),
                        A.Fake<IClientAccountClient>(),
                        A.Fake<BaseSettings>());
                });

            "When I authenticate"
                .x(() =>
                {
                    var request = new AuthRequestModel()
                    {
                        Email = "example@example.com",
                        Password = "pasword1234"
                    };

                    result = clientController.Auth(request);
                });

            "Then the result is OK"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                });
        }

        [Scenario]
        public void UserInfoHappyPath(IPersonalDataService personalDataService, IRequestContext requestContext, ClientController clientController, Task<IActionResult> result)
        {
            "Given a personal data service"
                .x(() =>
                {
                    personalDataService = A.Fake<IPersonalDataService>();
                    A.CallTo(() => personalDataService.GetAsync("clientId")).Returns(Task.FromResult(A.Fake<IPersonalData>()));
                });

            "And a request context"
                .x(() =>
                  {
                      requestContext = A.Fake<IRequestContext>();
                      A.CallTo(() => requestContext.ClientId).Returns("clientId");
                  });

            "And a client controller"
                .x(() =>
                {
                    clientController = new ClientController(
                        A.Fake<ILog>(),
                        A.Fake<ILykkePrincipal>(),
                        A.Fake<IClientSessionsClient>(),
                        A.Fake<ILykkeRegistrationClient>(),
                        A.Fake<ClientAccountLogic>(),
                        requestContext,
                        personalDataService,
                        A.Fake<IKycStatusService>(),
                        A.Fake<IClientAccountClient>(),
                        A.Fake<BaseSettings>());
                });

            "When I authenticate"
                .x(() =>
                {
                    var request = new AuthRequestModel()
                    {
                        Email = "example@example.com",
                        Password = "pasword1234"
                    };

                    result = clientController.Auth(request);
                });

            "Then the result is OK"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                });
        }

        [Scenario]
        public void FeaturesHappyPath(
            IClientAccountClient clientAccountService, 
            IClientSessionsClient clientSessionsClient,
            ILykkePrincipal lykkePrincipal,
            IRequestContext requestContext,
            ClientController clientController,
            Task<FeaturesResponseModel> result)
        {
            "Given a client account service"
                .x(() =>
                {
                    clientAccountService = A.Fake<IClientAccountClient>();
                    A.CallTo(() => clientAccountService.GetFeaturesAsync("clientId")).Returns(Task.FromResult(new FeaturesSettingsModel()));
                });

            "And a client account service"
                .x(() =>
                {
                    clientSessionsClient = A.Fake<IClientSessionsClient>();
                    A.CallTo(() => clientSessionsClient.GetTradingSession("token")).Returns(Task.FromResult((TradingSessionModel)null));
                });

            "And a lykke principal"
                .x(() =>
                {
                    lykkePrincipal = A.Fake<ILykkePrincipal>();
                    A.CallTo(() => lykkePrincipal.GetToken()).Returns("token");
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And a client controller"
                .x(() =>
                {
                    clientController = new ClientController(
                        A.Fake<ILog>(),
                        A.Fake<ILykkePrincipal>(),
                        clientSessionsClient,
                        A.Fake<ILykkeRegistrationClient>(),
                        A.Fake<ClientAccountLogic>(),
                        requestContext,
                        A.Fake<IPersonalDataService>(),
                        A.Fake<IKycStatusService>(),
                        clientAccountService,
                        A.Fake<BaseSettings>());
                });

            "When I request features"
                .x(() =>
                {
                    result = clientController.Features();
                });

            "Then the result is OK"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(FeaturesResponseModel));
                    result.Result.Should().NotBeNull();
                });
        }
    }
}
