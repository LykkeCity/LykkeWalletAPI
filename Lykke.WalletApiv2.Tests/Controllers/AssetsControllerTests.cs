// Copyright 2017 Lykke Corp.
// See LICENSE file in the project root for full license information.

namespace LykkeApi2.Tests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using FakeItEasy;
    using FluentAssertions;
    using Lykke.Service.Assets.Client;
    using Lykke.Service.Assets.Client.Models;
    using Lykke.Service.ClientAccount.Client;
    using LykkeApi2.Controllers;
    using LykkeApi2.Infrastructure;
    using Microsoft.AspNetCore.Mvc;
    using Xbehave;

    public class AssetsControllerTests
    {
        [Scenario(Skip = "TODO (Rachael) CachedDataDictionary cannot be faked")]
        public void GetAssets(
            IAssetsService assetsService,
            CachedDataDictionary<string, Asset> assetsCache,
            IRequestContext requestContext,
            AssetsController assetsController, 
            Task<IActionResult> result)
        {
            "Given an asset cache"
                .x(() =>
                {
                    assetsCache = A.Fake<CachedDataDictionary<string, Asset>>();
                    IEnumerable<Asset> assets = null;
                    A.CallTo(() => assetsCache.Values()).Returns(Task.FromResult(assets));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an assets controller"
                .x(() =>
                {
                    assetsController = new AssetsController(                  
                        A.Fake<IAssetsService>(),
                        assetsCache,
                        A.Fake<IClientAccountSettingsClient>(),
                        requestContext);
                });

            "When I request all assets"
                .x(() =>
                {
                    result = assetsController.Get();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((OkObjectResult)result.Result).Value.Should().BeOfType(typeof(Models.AssetsModel));
                    ((Models.AssetsModel)((OkObjectResult)result.Result).Value).Assets.Count().Should().Be(0);
                });
        }
    }
}
